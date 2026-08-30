using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
using RMS.Application.Features.Requisitions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.Requisitions.Commands.CreateRequisition;

public class CreateRequisitionCommandHandler : IRequestHandler<CreateRequisitionCommand, RequisitionDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApprovalWorkflowEngine _approvalWorkflowEngine;
    private readonly PolicyEvaluationService _policyEvaluationService;

    public CreateRequisitionCommandHandler(
        IRequisitionRepository requisitionRepository,
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ApprovalWorkflowEngine approvalWorkflowEngine,
        PolicyEvaluationService policyEvaluationService)
    {
        _requisitionRepository = requisitionRepository;
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _approvalWorkflowEngine = approvalWorkflowEngine;
        _policyEvaluationService = policyEvaluationService;
    }

    public async Task<RequisitionDto> Handle(CreateRequisitionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";
        var actorRole = _currentUser.Role?.ToString();

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (!category.IsActive)
        {
            throw new ConflictException("This category is not currently active and cannot be used for a new requisition.");
        }

        RequisitionFieldValidation.EnsureValid(category, request.FieldValues, request.CostCenterId, request.ProjectCode, request.Submit);

        var requisition = new Requisition
        {
            CompanyId = companyId,
            CategoryId = request.CategoryId,
            CategoryVersionNumber = category.CurrentVersionNumber,
            RequestedByUserId = userId,
            Priority = request.Priority,
            NeedByDate = request.NeedByDate,
            EstimatedCost = request.EstimatedCost,
            Justification = request.Justification,
            UrgencyJustification = request.UrgencyJustification,
            CostCenterId = request.CostCenterId,
            ProjectCode = request.ProjectCode,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var draftEntry = new RequisitionStatusHistory
        {
            RequisitionId = requisition.Id,
            FromStatus = null,
            ToStatus = Domain.Enums.RequisitionStatus.Draft,
            ActorUserId = userId,
            ActorName = actorName,
            ActorRole = actorRole,
        };
        requisition.StatusHistory.Add(draftEntry);

        var resolvedItems = RequisitionFieldValidation.ResolveItems(category, request.Items, request.Submit);

        _requisitionRepository.Add(requisition);
        _requisitionRepository.AddStatusHistory(draftEntry);
        _requisitionRepository.ReplaceItems(requisition, resolvedItems);
        _requisitionRepository.ReplaceFieldValues(requisition, request.FieldValues
            .Select(v => new RequisitionFieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value })
            .ToList());

        if (request.Submit)
        {
            // Feature 4: backend-authoritative re-check right before the requisition actually enters
            // the workflow - never trust an earlier/frontend-only eligibility check. One check per
            // distinct (CategoryId, CategoryItemId) pair among the resolved items (an item with a null
            // CategoryItemId is checked at the category level only). Blocks the whole submit on the
            // first failure - nothing has been saved yet (SaveChangesAsync hasn't run), so throwing
            // here leaves no partial state.
            foreach (var categoryItemId in resolvedItems.Select(i => i.CategoryItemId).Distinct())
            {
                var eligibility = await _policyEvaluationService.CheckAsync(userId, request.CategoryId, categoryItemId, cancellationToken);
                if (!eligibility.IsEligible)
                {
                    throw new ConflictException(eligibility.Reason ?? "You are not currently eligible to request this item.");
                }
            }

            var year = DateTime.UtcNow.Year;
            var sequence = await _requisitionRepository.CountNumberedInYearAsync(year, cancellationToken) + 1;
            var submitEntry = requisition.Submit(RequisitionNumberFormatter.Format(year, sequence), userId, actorName, actorRole);
            _requisitionRepository.AddStatusHistory(submitEntry);

            // Feature 3: resolves the active workflow for this company/category, creates the approval
            // process, and transitions Submitted -> UnderReview (or straight to Approved if every stage
            // is skipped) - see ApprovalWorkflowEngine.ResolveAndStart.
            await _approvalWorkflowEngine.ResolveAndStartAsync(requisition, userId, actorName, actorRole, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            request.Submit ? "RequisitionSubmitted" : "RequisitionDraftSaved",
            nameof(Requisition), requisition.Id, $"CategoryId={requisition.CategoryId}", cancellationToken);

        var saved = await _requisitionRepository.GetByIdAsync(requisition.Id, cancellationToken) ?? requisition;
        return RequisitionDto.FromEntity(saved);
    }
}
