namespace WarehouseManagement.Domain.Common;

/// <summary>
/// Base entity representing the core identity of any persisted entity in the system.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
