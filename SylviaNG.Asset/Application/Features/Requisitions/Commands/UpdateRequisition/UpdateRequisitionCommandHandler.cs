using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.Requisitions;
using RMS.Application.Features.Requisitions.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;
using RMS.Domain.Enums;

namespace RMS.Application.Features.Requisitions.Commands.UpdateRequisition;

public class UpdateRequisitionCommandHandler : IRequestHandler<UpdateRequisitionCommand, RequisitionDto>
{
    private readonly IRequisitionRepository _requisitionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRequisitionCommandHandler(
        IRequisitionRepository requisitionRepository,
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork)
    {
        _requisitionRepository = requisitionRepository;
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequisitionDto> Handle(UpdateRequisitionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException();
        var actorName = _currentUser.FullName ?? "Unknown";
        var actorRole = _currentUser.Role?.ToString();

        var requisition = await _requisitionRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Requisition), request.Id);

        if (requisition.RequestedByUserId != userId)
        {
            throw new ForbiddenException();
        }

        // FR-RR-010: a SentBack requisition can be amended and resubmitted, same as a Draft can be
        // edited and submitted. Any other status is no longer editable by the requestor.
        var isAmendment = requisition.Status == RequisitionStatus.SentBack;
        if (requisition.Status != RequisitionStatus.Draft && !isAmendment)
        {
            throw new ConflictException("This requisition has already been submitted and can no longer be edited.");
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(RequisitionCategory), request.CategoryId);

        if (!category.IsActive)
        {
            throw new ConflictException("This category is not currently active and cannot be used for a requisition.");
        }

        RequisitionFieldValidation.EnsureValid(category, request.FieldValues, request.CostCenterId, request.ProjectCode, request.Submit);

        // FR-RR-010: field-by-field before -> after diff for the amendment audit trail. Captured
        // before mutation; only meaningful (and only logged) for an actual SentBack amendment -
        // a plain Draft edit already gets its own "RequisitionDraftSaved"/"RequisitionSubmitted" entry.
        var diff = isAmendment ? BuildDiff(requisition, request) : null;

        requisition.CategoryId = request.CategoryId;
        requisition.CategoryVersionNumber = category.CurrentVersionNumber;
        requisition.Priority = request.Priority;
        requisition.NeedByDate = request.NeedByDate;
        requisition.EstimatedCost = request.EstimatedCost;
        requisition.Justification = request.Justification;
        requisition.UrgencyJustification = request.UrgencyJustification;
        requisition.CostCenterId = request.CostCenterId;
        requisition.ProjectCode = request.ProjectCode;
        requisition.UpdatedByUserId = userId;
        requisition.UpdatedAtUtc = DateTime.UtcNow;

        var resolvedItems = RequisitionFieldValidation.ResolveItems(category, request.Items, request.Submit);
        _requisitionRepository.ReplaceItems(requisition, resolvedItems);
        _requisitionRepository.ReplaceFieldValues(requisition, request.FieldValues
            .Select(v => new RequisitionFieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value })
            .ToList());

        if (request.Submit)
        {
            RequisitionStatusHistory transitionEntry;
            if (isAmendment)
            {
                transitionEntry = requisition.Resubmit(userId, actorName, actorRole, request.ResubmitComment);
            }
            else
            {
                var year = DateTime.UtcNow.Year;
                var sequence = await _requisitionRepository.CountNumberedInYearAsync(year, cancellationToken) + 1;
                transitionEntry = requisition.Submit(RequisitionNumberFormatter.Format(year, sequence), userId, actorName, actorRole);
            }
            // requisition was loaded (already tracked), so the new StatusHistory entry needs to be
            // registered explicitly - see Requisition.Submit's remarks.
            _requisitionRepository.AddStatusHistory(transitionEntry);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (isAmendment)
        {
            await _auditLogger.LogAsync(
                "RequisitionAmended", nameof(Requisition), requisition.Id,
                diff is { Length: > 0 } ? $"Changes: {diff}" : "No field changes.", cancellationToken);
        }
        else
        {
            await _auditLogger.LogAsync(
                request.Submit ? "RequisitionSubmitted" : "RequisitionDraftSaved",
                nameof(Requisition), requisition.Id, $"CategoryId={requisition.CategoryId}", cancellationToken);
        }

        var saved = await _requisitionRepository.GetByIdAsync(requisition.Id, cancellationToken) ?? requisition;
        return RequisitionDto.FromEntity(saved);
    }

    private static string BuildDiff(Requisition before, UpdateRequisitionCommand after)
    {
        var changes = new List<string>();

        void Track<T>(string field, T oldValue, T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
            {
                changes.Add($"{field}: '{oldValue}' -> '{newValue}'");
            }
        }

        Track(nameof(Requisition.Priority), before.Priority, after.Priority);
        Track(nameof(Requisition.NeedByDate), before.NeedByDate.Date, after.NeedByDate.Date);
        Track(nameof(Requisition.EstimatedCost), before.EstimatedCost, after.EstimatedCost);
        Track(nameof(Requisition.Justification), before.Justification, after.Justification);
        Track(nameof(Requisition.CostCenterId), before.CostCenterId, after.CostCenterId);
        Track(nameof(Requisition.ProjectCode), before.ProjectCode, after.ProjectCode);

        return string.Join("; ", changes);
    }
}
