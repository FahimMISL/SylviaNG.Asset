namespace RMS.Domain.Enums;

/// <summary>
/// Feature 10 (US-031): the modules a permission can be granted against. File Management is
/// deliberately omitted - it doesn't exist as a built feature yet; extend this enum when it does.
/// Manpower has no separate code path today (a manpower requisition is a plain Requisition row with a
/// special category) - its row in the matrix is informational until/unless a later feature gives it
/// its own endpoints. Search (Feature 11) reuses the Requisition model directly, same as Manpower.
/// </summary>
public enum PermissionModule
{
    RequisitionSetup = 0,
    RequisitionRequests = 1,
    ApprovalWorkflow = 2,
    EligibilityPolicy = 3,
    Procurement = 4,
    Manpower = 5,
    Reporting = 6,
    Audit = 7,
    Notifications = 8,
    Rbac = 9,
    Dashboard = 10,
    Search = 11,
}
