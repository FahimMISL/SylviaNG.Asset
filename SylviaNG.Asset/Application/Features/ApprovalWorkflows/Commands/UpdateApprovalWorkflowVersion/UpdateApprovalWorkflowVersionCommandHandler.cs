using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Features.ApprovalWorkflows.Mappings;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.UpdateApprovalWorkflowVersion;

public class UpdateApprovalWorkflowVersionCommandHandler : IRequestHandler<UpdateApprovalWorkflowVersionCommand, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateApprovalWorkflowVersionCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalWorkflowDto> Handle(UpdateApprovalWorkflowVersionCommand request, CancellationToken cancellationToken)
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
            throw new ConflictException("This version has already been published and can no longer be edited. Create a new draft version instead.");
        }

        version.RoutingMode = request.RoutingMode;
        version.AppliesToAllCategories = request.AppliesToAllCategories;
        version.Notes = request.Notes;
        version.UpdatedByUserId = _currentUser.UserId;
        version.UpdatedAtUtc = DateTime.UtcNow;

        var newStages = ApprovalWorkflowStageMapper.ToEntities(request.Stages);
        _workflowRepository.ReplaceVersionStages(version, newStages);

        _workflowRepository.ReplaceVersionCategoryLinks(
            version, request.AppliesToAllCategories ? new List<Guid>() : request.CategoryIds.Distinct().ToList());

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalWorkflowVersionUpdated", nameof(ApprovalWorkflowVersion), version.Id,
            $"WorkflowId={workflow.Id}, VersionNumber={version.VersionNumber}", cancellationToken);

        var refreshed = await _workflowRepository.GetByIdAsync(workflow.Id, cancellationToken) ?? workflow;
        return ApprovalWorkflowDto.FromEntity(refreshed);
    }
}
