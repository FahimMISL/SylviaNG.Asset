namespace RMS.Application.Features.Dashboard.DTOs;

/// <summary>Feature 12 (HR Manager dashboard). "Position" is the category item's admin-defined name -
/// same terminology Feature 6's manpower form already uses.</summary>
public record PositionQuantityDto(string PositionName, int Quantity);

/// <summary>One manpower requisition, with its own quantities aggregated correctly - never treated as
/// multiple independent requisitions just because it has multiple Position x Quantity lines.</summary>
public record ManpowerRequisitionSummaryDto(
    Guid Id, string? RequisitionNumber, string RequesterName, string Status, DateTime CreatedAtUtc,
    int TotalQuantity, List<PositionQuantityDto> Positions);

public record ManpowerSummaryDto(
    List<ManpowerRequisitionSummaryDto> RecentRequisitions, int TotalPositionsRequested, List<PositionQuantityDto> TopPositions);
