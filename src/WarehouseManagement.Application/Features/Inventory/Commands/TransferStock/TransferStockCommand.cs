using MediatR;

namespace WarehouseManagement.Application.Features.Inventory.Commands.TransferStock;

public record StockTransferResultDto(
    bool Success,
    int ProductId,
    string ProductName,
    string ProductSKU,
    int FromWarehouseId,
    string FromWarehouseName,
    int ToWarehouseId,
    string ToWarehouseName,
    int Quantity,
    DateTime TransferredAt
);

public record TransferStockCommand(
    int FromWarehouseId,
    int ToWarehouseId,
    int ProductId,
    int Quantity,
    string? ReferenceId = null,
    string? Notes = null
) : IRequest<StockTransferResultDto>;
