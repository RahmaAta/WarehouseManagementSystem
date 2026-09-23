namespace WarehouseManagement.Domain.Exceptions;

/// <summary>
/// Base exception for all business domain invariant violations.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
