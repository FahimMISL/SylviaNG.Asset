using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;

namespace RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflows;

public record GetApprovalWorkflowsQuery(bool? IsActive) : IRequest<List<ApprovalWorkflowSummaryDto>>;
