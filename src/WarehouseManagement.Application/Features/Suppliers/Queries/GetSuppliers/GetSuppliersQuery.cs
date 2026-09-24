using MediatR;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Suppliers.DTOs;

namespace WarehouseManagement.Application.Features.Suppliers.Queries.GetSuppliers;

public record GetSuppliersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool IncludeInactive = false
) : IRequest<PaginatedList<SupplierDto>>;
