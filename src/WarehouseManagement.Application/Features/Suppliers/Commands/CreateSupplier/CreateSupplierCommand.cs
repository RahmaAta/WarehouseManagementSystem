using MediatR;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.CreateSupplier;

public record CreateSupplierCommand(
    string Name,
    string Email,
    string PhoneNumber,
    string? ContactPerson = null,
    string? Address = null
) : IRequest<SupplierDto>;
