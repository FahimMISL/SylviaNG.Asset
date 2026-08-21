using MediatR;
using RMS.Application.Features.Procurement.DTOs;

namespace RMS.Application.Features.Procurement.Queries.GetProcurementRequisitions;

/// <summary>Every requisition currently in the procurement pipeline for the caller's company - the
/// frontend groups these client-side into Queue/Processing/Completed tabs, same as
/// pending-approvals/my-requisitions already filter client-side after one fetch.</summary>
public record GetProcurementRequisitionsQuery : IRequest<List<ProcurementQueueItemDto>>;
