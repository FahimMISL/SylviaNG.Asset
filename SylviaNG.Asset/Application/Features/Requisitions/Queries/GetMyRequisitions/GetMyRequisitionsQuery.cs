using MediatR;
using RMS.Application.Features.Requisitions.DTOs;

namespace RMS.Application.Features.Requisitions.Queries.GetMyRequisitions;

/// <summary>Backs "My Requisitions" / "Continue Draft" - the current user's own requisitions.</summary>
public record GetMyRequisitionsQuery : IRequest<List<RequisitionSummaryDto>>;
