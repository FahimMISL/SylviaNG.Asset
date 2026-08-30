using MediatR;
using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Features.ApprovalWorkflows.DTOs;
using RMS.Application.Interfaces;

namespace RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflows;

public class GetApprovalWorkflowsQueryHandler : IRequestHandler<GetApprovalWorkflowsQuery, List<ApprovalWorkflowSummaryDto>>
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly ICurrentUserService _currentUser;

    public GetApprovalWorkflowsQueryHandler(IApprovalWorkflowRepository workflowRepository, ICurrentUserService currentUser)
    {
        _workflowRepository = workflowRepository;
        _currentUser = currentUser;
    }

    public async Task<List<ApprovalWorkflowSummaryDto>> Handle(GetApprovalWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException();
        var workflows = await _workflowRepository.GetAllAsync(companyId, request.IsActive, cancellationToken);
        return workflows.Select(ApprovalWorkflowSummaryDto.FromEntity).ToList();
    }
}
