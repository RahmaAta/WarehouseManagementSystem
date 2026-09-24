namespace WarehouseManagement.Domain.Exceptions;

/// <summary>
/// Thrown when an order state transition violates the business lifecycle workflow.
/// </summary>
public class InvalidOrderStateException : DomainException
{
    public InvalidOrderStateException(string orderType, string currentStatus, string targetStatus)
        : base($"Cannot transition {orderType} from status '{currentStatus}' to '{targetStatus}'.")
    {
    }
}
