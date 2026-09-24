namespace WarehouseManagement.Domain.Exceptions;

/// <summary>
/// Thrown when a price or monetary value is set below zero.
/// </summary>
public class NegativePriceException : DomainException
{
    public decimal AttemptedPrice { get; }

    public NegativePriceException(decimal price)
        : base($"Product price cannot be negative. Attempted price: {price:C}.")
    {
        AttemptedPrice = price;
    }
}
