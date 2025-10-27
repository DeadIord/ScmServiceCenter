using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public sealed class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> in_builder)
    {
        in_builder.ToTable("TicketAttachments");

        in_builder.HasKey(a => a.Id);

        in_builder.Property(a => a.FileName)
            .HasMaxLength(256)
            .IsRequired();

        in_builder.Property(a => a.ContentType)
            .HasMaxLength(128)
            .IsRequired();

        in_builder.Property(a => a.Length)
            .IsRequired();

        in_builder.Property(a => a.Content)
            .IsRequired();
    }
}
