namespace RMS.Application.Features.Reporting.DTOs;

/// <summary>US-022: one row of the Operational Requisition Report. ApprovalStatus and
/// ProcurementStatus are human-readable summaries derived from Requisition.Status - not new stored
/// fields, see ReportingCalculations.</summary>
public record OperationalReportRowDto(
    Guid RequisitionId,
    string? RequisitionNumber,
    string RequesterName,
    string? Department,
    string CategoryName,
    string ItemsSummary,
    int TotalQuantity,
    string Status,
    string Priority,
    DateTime RequestDate,
    string ApprovalStatus,
    string? ProcurementStatus,
    DateTime? NeedByDate,
    double? ProcessingDays);

public record MonthlyTrendPointDto(string MonthLabel, int SubmittedCount);

/// <summary>US-025: CEO Executive Summary. Deliberately has no budget/vendor/inventory/finance-rule
/// field anywhere - none of that data exists anywhere in this system to report on. EligibilityBlockNote
/// discloses a real, confirmed limitation (blocked submissions are never persisted - see
/// CreateRequisitionCommandHandler) rather than fabricating a blocked-request count.</summary>
public record ExecutiveSummaryDto(
    int TotalRequisitions,
    int PendingCount,
    int ApprovedCount,
    int RejectedCount,
    int FulfilledCount,
    int PendingApprovalsCount,
    int ProcurementActiveCount,
    int ManpowerRequisitionCount,
    int ManpowerTotalPositionsRequested,
    double? AverageApprovalDays,
    List<MonthlyTrendPointDto> MonthlyTrend,
    string EligibilityBlockNote);
