using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Number)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(o => o.Number)
            .IsUnique();

        builder.Property(o => o.ClientName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(o => o.ClientPhone)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Device)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(o => o.Serial)
            .HasMaxLength(64);

        builder.Property(o => o.Defect)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.ClientAccessToken)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(o => o.Status);

        builder.HasIndex(o => o.AccountId);

        builder.HasIndex(o => o.ContactId);

        builder.HasIndex(o => o.SLAUntil);

        builder.Property(o => o.Priority)
            .HasConversion<int>();

        builder.Property(o => o.Status)
            .HasConversion<int>();

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(o => o.SLAUntil)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(o => o.Account)
            .WithMany()
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Contact)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.ContactId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
