using MediatR;
using WarehouseManagement.Application.Common.Models;
using WarehouseManagement.Application.Features.Customers.DTOs;

namespace WarehouseManagement.Application.Features.Customers.Queries.GetCustomers;

public record GetCustomersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool IncludeInactive = false
) : IRequest<PaginatedList<CustomerDto>>;
