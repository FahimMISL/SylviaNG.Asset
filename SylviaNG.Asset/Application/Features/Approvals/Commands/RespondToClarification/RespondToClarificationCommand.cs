using MediatR;

namespace RMS.Application.Features.Approvals.Commands.RespondToClarification;

public record RespondToClarificationCommand(Guid ApprovalId, string Comment) : IRequest;
