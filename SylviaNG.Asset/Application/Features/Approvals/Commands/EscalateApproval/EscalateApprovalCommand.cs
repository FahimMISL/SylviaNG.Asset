using MediatR;

namespace RMS.Application.Features.Approvals.Commands.EscalateApproval;

public record EscalateApprovalCommand(Guid ApprovalId, string Comment) : IRequest;
