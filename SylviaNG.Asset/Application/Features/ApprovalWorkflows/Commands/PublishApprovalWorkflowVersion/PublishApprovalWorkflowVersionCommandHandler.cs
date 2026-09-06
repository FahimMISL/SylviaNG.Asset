using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.PublishApprovalWorkflowVersion;

public class PublishApprovalWorkflowVersionCommandHandler : IRequestHandler<PublishApprovalWorkflowVersionCommand, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public PublishApprovalWorkflowVersionCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalWorkflowDto> Handle(PublishApprovalWorkflowVersionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalWorkflow), request.WorkflowId);
        if (workflow.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        var version = workflow.Versions.FirstOrDefault(v => v.Id == request.VersionId)
            ?? throw new NotFoundException(nameof(ApprovalWorkflowVersion), request.VersionId);

        if (version.IsPublished)
        {
            throw new ConflictException("This version is already published.");
        }

        if (version.Stages.Count == 0)
        {
            throw new ConflictException("Cannot publish a workflow version with no stages.");
        }

        version.IsPublished = true;
        version.PublishedAtUtc = DateTime.UtcNow;
        version.UpdatedByUserId = _currentUser.UserId;
        version.UpdatedAtUtc = DateTime.UtcNow;

        // Once published this becomes THE current version - existing requisitions already resolved
        // against an earlier version keep it forever (RequisitionApprovalProcess.ApprovalWorkflowVersionId
        // snapshot), so bumping this pointer only affects new requisitions from now on. Older published
        // versions stay IsPublished=true (immutable historical fact) but no longer resolve for new work.
        workflow.CurrentVersionNumber = version.VersionNumber;
        workflow.UpdatedByUserId = _currentUser.UserId;
        workflow.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalWorkflowVersionPublished", nameof(ApprovalWorkflowVersion), version.Id,
            $"WorkflowId={workflow.Id}, VersionNumber={version.VersionNumber}", cancellationToken);

        return ApprovalWorkflowDto.FromEntity(workflow);
    }
}
