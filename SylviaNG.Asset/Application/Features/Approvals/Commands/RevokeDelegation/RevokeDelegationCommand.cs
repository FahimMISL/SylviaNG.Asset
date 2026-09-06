using MediatR;

namespace RMS.Application.Features.Approvals.Commands.RevokeDelegation;

public record RevokeDelegationCommand(Guid Id, string Reason) : IRequest;
