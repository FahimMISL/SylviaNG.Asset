using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Interfaces;
using RMS.Domain.Entities;

namespace RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflowById;

public class GetApprovalWorkflowByIdQueryHandler : IRequestHandler<GetApprovalWorkflowByIdQuery, ApprovalWorkflowDto>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;

    public GetApprovalWorkflowByIdQueryHandler(IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
    }

    public async Task<ApprovalWorkflowDto> Handle(GetApprovalWorkflowByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();

        var workflow = await _workflowRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalWorkflow), request.Id);

        if (workflow.CompanyId != companyId)
        {
            throw new ForbiddenException();
        }

        return ApprovalWorkflowDto.FromEntity(workflow);
    }
}
