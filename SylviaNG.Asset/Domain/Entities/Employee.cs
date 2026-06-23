using SylviaNG.Assets.SharedKernel.Audit;

namespace SylviaNG.Assets.Domain.Entities;

/// <summary>
/// Synced employee data from the Core/Employee microservice via Kafka.
/// Used for interviewer lookups and candidate-to-employee conversion.
/// </summary>
public class Employee : Audit
{
    public long EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public long? DepartmentId { get; set; }
    public long? DesignatioId { get; set; }
    public long? SiteId { get; set; }
    public long? RFId { get; set; }
}
