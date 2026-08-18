using MediatR;

namespace RMS.Application.Features.Approvals.Commands.SendBackApproval;

public record SendBackApprovalCommand(Guid ApprovalId, string Comment) : IRequest;
