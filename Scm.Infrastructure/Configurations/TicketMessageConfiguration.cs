using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> in_builder)
    {
        in_builder.ToTable("TicketMessages");

        in_builder.HasKey(m => m.Id);

        in_builder.Property(m => m.Subject)
            .HasMaxLength(512)
            .IsRequired();

        in_builder.Property(m => m.BodyHtml)
            .HasColumnType("text")
            .IsRequired();

        in_builder.Property(m => m.BodyText)
            .HasColumnType("text");

        in_builder.Property(m => m.SenderName)
            .HasMaxLength(128);

        in_builder.Property(m => m.ExternalId)
            .HasMaxLength(256)
            .IsRequired();

        in_builder.Property(m => m.ExternalReferences)
            .HasColumnType("text");

        in_builder.Property(m => m.CreatedByUserId)
            .HasMaxLength(450);

        in_builder.Property(m => m.SentAtUtc)
            .HasColumnType("timestamp with time zone");

        in_builder.HasIndex(m => m.ExternalId)
            .IsUnique();

        in_builder.HasMany(m => m.Attachments)
            .WithOne(a => a.TicketMessage)
            .HasForeignKey(a => a.TicketMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
