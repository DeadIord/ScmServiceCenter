using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> in_builder)
    {
        in_builder.ToTable("Tickets");

        in_builder.HasKey(t => t.Id);

        in_builder.Property(t => t.Subject)
            .HasMaxLength(256)
            .IsRequired();

        in_builder.Property(t => t.ClientEmail)
            .HasMaxLength(256)
            .IsRequired();

        in_builder.Property(t => t.ClientName)
            .HasMaxLength(128);

        in_builder.Property(t => t.ExternalThreadId)
            .HasMaxLength(256);

        in_builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        in_builder.Property(t => t.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        in_builder.Property(t => t.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        in_builder.HasIndex(t => t.Status);
        in_builder.HasIndex(t => t.ClientEmail);
        in_builder.HasIndex(t => t.UpdatedAtUtc);

        in_builder.HasMany(t => t.Messages)
            .WithOne(m => m.Ticket)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
