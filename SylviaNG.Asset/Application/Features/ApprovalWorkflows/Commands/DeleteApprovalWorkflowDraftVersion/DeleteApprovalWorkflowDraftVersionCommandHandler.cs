using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.DeleteApprovalWorkflowDraftVersion;

public class DeleteApprovalWorkflowDraftVersionCommandHandler : IRequestHandler<DeleteApprovalWorkflowDraftVersionCommand>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteApprovalWorkflowDraftVersionCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteApprovalWorkflowDraftVersionCommand request, CancellationToken cancellationToken)
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
            throw new ConflictException("A published version cannot be deleted.");
        }

        _workflowRepository.RemoveVersion(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalWorkflowDraftVersionDeleted", nameof(ApprovalWorkflowVersion), version.Id,
            $"WorkflowId={workflow.Id}, VersionNumber={version.VersionNumber}", cancellationToken);
    }
}
