using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseManagement.Domain.Entities;

namespace WarehouseManagement.Infrastructure.Persistence.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");

        builder.HasKey(so => so.Id);

        builder.Property(so => so.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(so => so.OrderNumber)
            .IsUnique();

        builder.Property(so => so.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(so => so.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(so => so.ConfirmedBy)
            .HasMaxLength(100);

        builder.Property(so => so.CompletedBy)
            .HasMaxLength(100);

        builder.HasOne(so => so.Customer)
            .WithMany(c => c.SalesOrders)
            .HasForeignKey(so => so.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(so => so.Warehouse)
            .WithMany()
            .HasForeignKey(so => so.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation collection backed by private field _items
        builder.HasMany(so => so.Items)
            .WithOne(soi => soi.SalesOrder)
            .HasForeignKey(soi => soi.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(so => so.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
{
    public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
    {
        builder.ToTable("SalesOrderItems");

        builder.HasKey(soi => soi.Id);

        builder.Property(soi => soi.Quantity)
            .IsRequired();

        builder.Property(soi => soi.UnitPrice)
            .HasPrecision(18, 2);

        builder.HasOne(soi => soi.Product)
            .WithMany()
            .HasForeignKey(soi => soi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
