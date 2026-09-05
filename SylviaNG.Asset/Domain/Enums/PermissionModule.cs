namespace RMS.Domain.Enums;

/// <summary>
/// Feature 10 (US-031): the modules a permission can be granted against. Manpower has no separate
/// code path today (a manpower requisition is a plain Requisition row with a special category) - its
/// row in the matrix is informational until/unless a later feature gives it its own endpoints. Search
/// (Feature 11) and FileManagement (Feature 13) both reuse the Requisition model directly, same as
/// Manpower - row-level access for files is still RequisitionAccessHelper, this module exists so the
/// matrix UI can display/manage it, not because a new endpoint checks it directly.
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
    FileManagement = 12,
}
