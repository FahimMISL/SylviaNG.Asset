using MediatR;
using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Application.Features.Approvals.Queries.GetDelegations;

/// <summary>SystemAdmin-only, all delegations in the company.</summary>
public record GetDelegationsQuery : IRequest<List<ApprovalDelegationDto>>;
