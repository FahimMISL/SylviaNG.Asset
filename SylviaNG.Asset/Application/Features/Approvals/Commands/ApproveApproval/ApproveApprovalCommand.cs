using MediatR;

namespace RMS.Application.Features.Approvals.Commands.ApproveApproval;

public record ApproveApprovalCommand(Guid ApprovalId, string Comment) : IRequest;
