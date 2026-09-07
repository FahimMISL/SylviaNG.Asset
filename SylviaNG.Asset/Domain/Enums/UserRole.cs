namespace RMS.Domain.Enums;

/// <summary>
/// Only SystemAdmin is used/seeded by the Feature 1 + foundation slice; the rest exist so the type
/// doesn't need to be reshaped when later features (Feature 12 - RBAC) light them up. FinanceOfficer
/// and StoreOfficer removed per supervisor's request to trim scope - Feature 5 (Procurement &amp;
/// Fulfillment) deliberately has no Store Officer/inventory concept and no Finance/Budget approval
/// role. Explicit values kept non-contiguous (skipping 3 and 6) rather than renumbered, so any old
/// data or logs referencing the removed roles' original integer values aren't silently reinterpreted
/// as a different role.
///
/// Manager (value 1) was renamed from LineManager - same role, same integer value, only the name
/// changed, so no data migration was needed: every functional role-typed column (Users.Role,
/// RolePermissions.Role, WorkflowApprovers.ApproverRole, ApprovalWorkflowSlaConfigurations.
/// EscalationApproverRole) stores this as an int, and 1 still means the same role it always did.
/// Historical text snapshots of the old name (AuditLogs.ActorRole and similar ActorRole columns)
/// intentionally still say "LineManager" for actions taken before the rename - that's correct,
/// not stale, the same way a job title change doesn't rewrite old paperwork.
/// </summary>
public enum UserRole
{
    Employee = 0,
    Manager = 1,
    DepartmentHead = 2,
    ProcurementOfficer = 4,
    HrManager = 5,
    SystemAdmin = 7,
    Ceo = 8,
}
