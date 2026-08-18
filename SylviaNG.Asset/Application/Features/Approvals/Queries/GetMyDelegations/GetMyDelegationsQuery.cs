using MediatR;
using RMS.Application.Features.Approvals.DTOs;

namespace RMS.Application.Features.Approvals.Queries.GetMyDelegations;

public record GetMyDelegationsQuery : IRequest<List<ApprovalDelegationDto>>;
