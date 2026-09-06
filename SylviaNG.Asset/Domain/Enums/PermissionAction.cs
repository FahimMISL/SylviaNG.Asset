namespace RMS.Domain.Enums;

/// <summary>Feature 10 (US-031). Not every action applies to every module (e.g. Approve only really
/// means something for RequisitionRequests/Manpower) - a row simply stays false for actions that don't
/// apply to that module, rather than the enum trying to model per-module applicability.</summary>
public enum PermissionAction
{
    View = 0,
    Create = 1,
    Edit = 2,
    Approve = 3,
    Delete = 4,
    Export = 5,
}
