using MediatR;

namespace RMS.Application.Features.Approvals.Commands.RequestClarification;

public record RequestClarificationCommand(Guid ApprovalId, string Comment) : IRequest;
