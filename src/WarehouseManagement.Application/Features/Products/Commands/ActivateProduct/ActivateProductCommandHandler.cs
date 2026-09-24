using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WarehouseManagement.Application.Common.Interfaces;

namespace WarehouseManagement.Application.Features.Products.Commands.ActivateProduct
{
    public class ActivateProductCommandHandler : IRequestHandler<ActivateProductCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public ActivateProductCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ActivateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {request.Id} was not found.");
            }

            product.Activate();
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
