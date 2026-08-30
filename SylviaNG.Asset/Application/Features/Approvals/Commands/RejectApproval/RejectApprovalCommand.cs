using MediatR;

namespace RMS.Application.Features.Approvals.Commands.RejectApproval;

public record RejectApprovalCommand(Guid ApprovalId, string Comment) : IRequest;
