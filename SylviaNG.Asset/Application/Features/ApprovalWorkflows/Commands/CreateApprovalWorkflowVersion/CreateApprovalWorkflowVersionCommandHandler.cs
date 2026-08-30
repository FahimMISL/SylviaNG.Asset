using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflowVersion;

public class CreateApprovalWorkflowVersionCommandHandler : IRequestHandler<CreateApprovalWorkflowVersionCommand, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApprovalWorkflowVersionCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalWorkflowDto> Handle(CreateApprovalWorkflowVersionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalWorkflow), request.WorkflowId);
        if (workflow.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        var source = workflow.Versions.FirstOrDefault(v => v.VersionNumber == workflow.CurrentVersionNumber && v.IsPublished)
            ?? throw new ConflictException("This workflow has no published version yet to clone. Publish a version first.");

        var nextVersionNumber = workflow.Versions.Count == 0 ? 1 : workflow.Versions.Max(v => v.VersionNumber) + 1;

        var clone = new ApprovalWorkflowVersion
        {
            ApprovalWorkflowId = workflow.Id,
            VersionNumber = nextVersionNumber,
            RoutingMode = source.RoutingMode,
            AppliesToAllCategories = source.AppliesToAllCategories,
            IsPublished = false,
            Notes = source.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        foreach (var stage in source.Stages.OrderBy(s => s.StageOrder))
        {
            var clonedStage = new ApprovalWorkflowStage
            {
                ApprovalWorkflowVersionId = clone.Id,
                StageOrder = stage.StageOrder,
                Name = stage.Name,
                CapturesEstimatedCost = stage.CapturesEstimatedCost,
            };

            foreach (var approver in stage.Approvers)
            {
                clonedStage.Approvers.Add(new WorkflowApprover
                {
                    ApprovalWorkflowStageId = clonedStage.Id,
                    ApproverType = approver.ApproverType,
                    ApproverRole = approver.ApproverRole,
                    ApproverUserId = approver.ApproverUserId,
                    FallbackApproverUserId = approver.FallbackApproverUserId,
                    IsRequired = approver.IsRequired,
                });
            }

            foreach (var condition in stage.Conditions)
            {
                clonedStage.Conditions.Add(new ApprovalWorkflowStageCondition
                {
                    ApprovalWorkflowStageId = clonedStage.Id,
                    ConditionType = condition.ConditionType,
                    MinCost = condition.MinCost,
                    MaxCost = condition.MaxCost,
                    CategoryId = condition.CategoryId,
                });
            }

            if (stage.Sla is not null)
            {
                clonedStage.Sla = new ApprovalWorkflowSlaConfiguration
                {
                    ApprovalWorkflowStageId = clonedStage.Id,
                    DurationValue = stage.Sla.DurationValue,
                    DurationUnit = stage.Sla.DurationUnit,
                    Reminder50PercentEnabled = stage.Sla.Reminder50PercentEnabled,
                    Reminder80PercentEnabled = stage.Sla.Reminder80PercentEnabled,
                    EscalateOnBreach = stage.Sla.EscalateOnBreach,
                    EscalationApproverRole = stage.Sla.EscalationApproverRole,
                    EscalationApproverUserId = stage.Sla.EscalationApproverUserId,
                };
            }

            clone.Stages.Add(clonedStage);
        }

        foreach (var link in source.CategoryLinks)
        {
            clone.CategoryLinks.Add(new ApprovalWorkflowCategoryLink
            {
                ApprovalWorkflowVersionId = clone.Id,
                RequisitionCategoryId = link.RequisitionCategoryId,
            });
        }

        // Added directly via the repository (not workflow.Versions.Add) - see
        // PublishCategoryCommandHandler.AddVersion for why this avoids EF Core's
        // insert-vs-update misdetection on an already-tracked parent.
        _workflowRepository.AddVersion(clone);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalWorkflowVersionCloned", nameof(ApprovalWorkflowVersion), clone.Id,
            $"WorkflowId={workflow.Id}, FromVersion={source.VersionNumber}, ToVersion={clone.VersionNumber}", cancellationToken);

        var refreshed = await _workflowRepository.GetByIdAsync(workflow.Id, cancellationToken) ?? workflow;
        return ApprovalWorkflowDto.FromEntity(refreshed);
    }
}
