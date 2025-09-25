using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("Parts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sku)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.Sku)
            .IsUnique();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StockQty)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.ReorderPoint)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.PriceIn)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.PriceOut)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Unit)
            .HasMaxLength(16)
            .IsRequired();
    }
}
