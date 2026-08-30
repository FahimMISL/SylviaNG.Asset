namespace RMS.Domain.Enums;

/// <summary>
/// Feature 10 (US-031): the modules a permission can be granted against. Search and File Management
/// are deliberately omitted - neither exists as a built feature yet (confirmed via investigation), so
/// there is nothing to gate; extend this enum when those features exist. Manpower has no separate code
/// path today (a manpower requisition is a plain Requisition row with a special category) - its row in
/// the matrix is informational until/unless a later feature gives it its own endpoints.
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
}
