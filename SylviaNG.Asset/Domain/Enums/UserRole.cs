namespace RMS.Domain.Enums;

/// <summary>
/// The 8 default roles per US-031 AC1. Only SystemAdmin is used/seeded by the
/// Feature 1 + foundation slice; the rest exist so the type doesn't need to be
/// reshaped when later features (Feature 12 - RBAC) light them up.
/// </summary>
public enum UserRole
{
    Employee = 0,
    LineManager = 1,
    DepartmentHead = 2,
    FinanceOfficer = 3,
    ProcurementOfficer = 4,
    HrManager = 5,
    StoreOfficer = 6,
    SystemAdmin = 7,
    Ceo = 8,
}
