namespace RMS.Domain.Entities;

/// <summary>An admin-defined Item/Type an employee can pick from for this category (e.g. "Laptop" under IT Equipment).</summary>
public class CategoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public RequisitionCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
