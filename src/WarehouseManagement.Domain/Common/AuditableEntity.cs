namespace WarehouseManagement.Domain.Common;

/// <summary>
/// Auditable entity tracking audit trail metadata across the lifecycle of business records.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}
