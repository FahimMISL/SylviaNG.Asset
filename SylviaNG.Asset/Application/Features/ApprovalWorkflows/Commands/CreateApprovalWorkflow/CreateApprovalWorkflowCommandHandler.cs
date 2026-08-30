using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Features.ApprovalWorkflows.Mappings;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Commands.CreateApprovalWorkflow;

public class CreateApprovalWorkflowCommandHandler : IRequestHandler<CreateApprovalWorkflowCommand, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApprovalWorkflowCommandHandler(
        IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser, IAuditLogger auditLogger, IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApprovalWorkflowDto> Handle(CreateApprovalWorkflowCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        if (await _workflowRepository.NameExistsAsync(companyId, request.Name, null, cancellationToken))
        {
            throw new ConflictException($"An approval workflow named '{request.Name}' already exists in your company.");
        }

        var workflow = new ApprovalWorkflow
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            IsActive = false, // admin previews/edits the draft version before activating, same convention as RequisitionCategory
            CurrentVersionNumber = 0,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var version = new ApprovalWorkflowVersion
        {
            ApprovalWorkflowId = workflow.Id,
            VersionNumber = 1,
            RoutingMode = request.RoutingMode,
            AppliesToAllCategories = request.AppliesToAllCategories,
            IsPublished = false,
            Notes = request.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        version.Stages = ApprovalWorkflowStageMapper.ToEntities(request.Stages);
        if (!request.AppliesToAllCategories)
        {
            foreach (var categoryId in request.CategoryIds.Distinct())
            {
                version.CategoryLinks.Add(new ApprovalWorkflowCategoryLink
                {
                    ApprovalWorkflowVersionId = version.Id,
                    RequisitionCategoryId = categoryId,
                });
            }
        }

        workflow.Versions.Add(version);

        _workflowRepository.Add(workflow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync("ApprovalWorkflowCreated", nameof(ApprovalWorkflow), workflow.Id, $"Name={workflow.Name}", cancellationToken);

        return ApprovalWorkflowDto.FromEntity(workflow);
    }
}
