namespace RMS.Application.Features.Requisitions;

/// <summary>FR-RR-004: REQ-{year}-{5-digit sequence}, e.g. REQ-2026-00123. Shared by Create/Update
/// (a Draft only gets a number the first time it's submitted, from either entry point).</summary>
public static class RequisitionNumberFormatter
{
    public static string Format(int year, int sequenceForYear) => $"REQ-{year}-{sequenceForYear:D5}";
}
