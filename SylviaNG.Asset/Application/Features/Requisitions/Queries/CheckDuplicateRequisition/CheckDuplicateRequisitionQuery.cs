using MediatR;

namespace RMS.Application.Features.Requisitions.Queries.CheckDuplicateRequisition;

/// <summary>
/// FR-RR-011: same category + same required date + same total quantity, submitted by
/// the same requestor within the last 7 days. A soft warning only - never blocks submission.
/// </summary>
public record CheckDuplicateRequisitionQuery(Guid CategoryId, DateTime NeedByDate, int TotalQuantity)
    : IRequest<DuplicateCheckResultDto>;

public record DuplicateCheckResultDto(bool IsPotentialDuplicate, string? ExistingRequisitionNumber, Guid? ExistingRequisitionId);
