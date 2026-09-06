using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.Services;
using RMS.Application.Features.EligibilityPolicies.Services;
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
    private readonly ApprovalWorkflowEngine _approvalWorkflowEngine;
    private readonly PolicyEvaluationService _policyEvaluationService;
    private readonly INotificationService _notificationService;

    public UpdateRequisitionCommandHandler(
        IRequisitionRepository requisitionRepository,
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUser,
        IAuditLogger auditLogger,
        IUnitOfWork unitOfWork,
        ApprovalWorkflowEngine approvalWorkflowEngine,
        PolicyEvaluationService policyEvaluationService,
        INotificationService notificationService)
    {
        _requisitionRepository = requisitionRepository;
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
        _approvalWorkflowEngine = approvalWorkflowEngine;
        _policyEvaluationService = policyEvaluationService;
        _notificationService = notificationService;
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

        var resolvedItems = RequisitionFieldValidation.ResolveItems(category, request.Items, request.Submit);

        if (request.Submit)
        {
            // Feature 4: same backend-authoritative re-check as CreateRequisitionCommandHandler - this
            // is the "Save Draft, then Submit later" / SentBack-resubmit path, so it needs its own
            // check too (the same "two submission paths" class of gap Feature 3 had to fix in both
            // handlers). Deliberately runs before requisition is mutated at all (see below) - nothing
            // has changed on the tracked entity yet, so throwing here leaves no partial state AND
            // Feature 8's audit-blocked log entry (which eagerly calls SaveChangesAsync itself) can't
            // accidentally commit an in-progress edit.
            foreach (var categoryItemId in resolvedItems.Select(i => i.CategoryItemId).Distinct())
            {
                var eligibility = await _policyEvaluationService.CheckAsync(userId, request.CategoryId, categoryItemId, cancellationToken);
                if (!eligibility.IsEligible)
                {
                    await _auditLogger.LogAsync(
                        "EligibilityCheckBlocked", nameof(Requisition), requisition.Id,
                        $"CategoryItemId={categoryItemId}; Reason={eligibility.Reason}", cancellationToken);
                    throw new ConflictException(eligibility.Reason ?? "You are not currently eligible to request this item.");
                }
            }
        }

        // FR-RR-010: field-by-field before -> after diff for the amendment audit trail. Captured
        // before mutation; only meaningful (and only logged) for an actual SentBack amendment -
        // a plain Draft edit already gets its own "RequisitionDraftSaved"/"RequisitionSubmitted" entry.
        var diff = isAmendment ? BuildDiff(requisition, request, category) : null;

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

        _requisitionRepository.ReplaceItems(requisition, resolvedItems);
        _requisitionRepository.ReplaceFieldValues(requisition, request.FieldValues
            .Select(v => new RequisitionFieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value })
            .ToList());

        var pendingNotifications = new List<NotificationRequest>();

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
                var sequence = await _requisitionRepository.GetHighestSequenceInYearAsync(year, cancellationToken) + 1;
                transitionEntry = requisition.Submit(RequisitionNumberFormatter.Format(year, sequence), userId, actorName, actorRole);
            }
            // requisition was loaded (already tracked), so the new StatusHistory entry needs to be
            // registered explicitly - see Requisition.Submit's remarks.
            _requisitionRepository.AddStatusHistory(transitionEntry);

            // Feature 3: this is the "Save Draft, then Submit later" / SentBack-resubmit path -
            // CreateRequisitionCommandHandler only covers a requisition submitted immediately. Without
            // this call the requisition would sit at Submitted forever with no ApprovalProcess and never
            // reach any approver's inbox.
            pendingNotifications.AddRange(isAmendment
                ? await _approvalWorkflowEngine.ResumeAfterSendBackAsync(requisition, userId, actorName, actorRole, cancellationToken)
                : await _approvalWorkflowEngine.ResolveAndStartAsync(requisition, userId, actorName, actorRole, cancellationToken));
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

        // Feature 9 (US-028): confirmation to the requestor that the (re)submission went through,
        // sent alongside whatever the approval engine queued - only now, after SaveChangesAsync.
        if (request.Submit)
        {
            pendingNotifications.Add(new NotificationRequest(
                requisition.CompanyId, userId, NotificationEventType.RequisitionSubmitted, requisition.Id,
                new Dictionary<string, string>
                {
                    ["RequisitionNumber"] = requisition.RequisitionNumber ?? "N/A",
                    ["Category"] = category.Name,
                    ["Item"] = resolvedItems.Count > 0 ? string.Join(", ", resolvedItems.Select(i => $"{i.ItemName} x{i.Quantity}")) : "N/A",
                }));
        }
        foreach (var notification in pendingNotifications)
        {
            await _notificationService.NotifyAsync(notification, cancellationToken);
        }

        var saved = await _requisitionRepository.GetByIdAsync(requisition.Id, cancellationToken) ?? requisition;
        return RequisitionDto.FromEntity(saved);
    }

    private static string BuildDiff(Requisition before, UpdateRequisitionCommand after, RequisitionCategory category)
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
        Track(nameof(Requisition.NeedByDate), before.NeedByDate?.Date, after.NeedByDate?.Date);
        Track(nameof(Requisition.EstimatedCost), before.EstimatedCost, after.EstimatedCost);
        Track(nameof(Requisition.Justification), before.Justification, after.Justification);
        Track(nameof(Requisition.CostCenterId), before.CostCenterId, after.CostCenterId);
        Track(nameof(Requisition.ProjectCode), before.ProjectCode, after.ProjectCode);

        // Feature 8: item/quantity changes weren't tracked at all before - the spec's own examples
        // ("Category/Type changed", "Quantity changed") name this explicitly. before.Items still
        // holds the pre-amendment list at this point (ReplaceItems runs after BuildDiff is called);
        // after.Items is resolved against the category's Items to get real names, same lookup
        // RequisitionFieldValidation.ResolveItems itself uses.
        var beforeSummary = string.Join(", ", before.Items.Select(i => $"{i.ItemName} x{i.Quantity}"));
        var afterSummary = string.Join(", ", after.Items.Select(i =>
        {
            var name = category.Items.FirstOrDefault(ci => ci.Id == i.CategoryItemId)?.Name ?? "Unknown item";
            return $"{name} x{i.Quantity}";
        }));
        if (!string.Equals(beforeSummary, afterSummary, StringComparison.Ordinal))
        {
            changes.Add($"Items: '{beforeSummary}' -> '{afterSummary}'");
        }

        return string.Join("; ", changes);
    }
}
