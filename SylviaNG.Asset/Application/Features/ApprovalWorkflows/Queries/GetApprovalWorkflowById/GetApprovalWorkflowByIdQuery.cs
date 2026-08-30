using MediatR;
using RMS.Application.Features.ApprovalWorkflows.DTOs;

namespace RMS.Application.Features.ApprovalWorkflows.Queries.GetApprovalWorkflowById;

public record GetApprovalWorkflowByIdQuery(Guid Id) : IRequest<ApprovalWorkflowDto>;
