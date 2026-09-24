using MediatR;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Commands.UpdateSupplier;

public record UpdateSupplierCommand(
    int Id,
    string Name,
    string Email,
    string PhoneNumber,
    string? ContactPerson = null,
    string? Address = null
) : IRequest<SupplierDto>;
