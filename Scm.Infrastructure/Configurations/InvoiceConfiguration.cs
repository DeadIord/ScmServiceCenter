using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.Amount)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Currency)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PaidAt)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Order)
            .WithMany(o => o.Invoices)
            .HasForeignKey(x => x.OrderId);
    }
}
