using MediatR;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Products.Commands.ActivateProduct;

public record ActivateProductCommand(int Id) : IRequest<bool>;
