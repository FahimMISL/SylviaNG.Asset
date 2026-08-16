using RMS.Domain.Common;

namespace RMS.Domain.Entities;

/// <summary>
/// Minimal multi-company scoping per US-042 - just enough for US-001 AC10
/// ("unique name within the same company"). Full group/branch hierarchy,
/// currency, and cross-company access is out of scope until US-042 itself.
/// </summary>
public class Company : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
