using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.ToTable("QuoteLines");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OrderId);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Qty)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Price)
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Kind)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.HasOne(x => x.Order)
            .WithMany(o => o.QuoteLines)
            .HasForeignKey(x => x.OrderId);
    }
}
