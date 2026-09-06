using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.SetApprovalWorkflowActiveState;

public class SetApprovalWorkflowActiveStateCommandHandler : IRequestHandler<SetApprovalWorkflowActiveStateCommand, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public SetApprovalWorkflowActiveStateCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalWorkflowDto> Handle(SetApprovalWorkflowActiveStateCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalWorkflow), request.WorkflowId);
        if (workflow.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        if (request.IsActive)
        {
            workflow.Activate();
        }
        else
        {
            workflow.Deactivate();
        }

        workflow.UpdatedByUserId = _currentUser.UserId;
        workflow.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            request.IsActive ? "ApprovalWorkflowActivated" : "ApprovalWorkflowDeactivated",
            nameof(ApprovalWorkflow), workflow.Id, null, cancellationToken);

        return ApprovalWorkflowDto.FromEntity(workflow);
    }
}
