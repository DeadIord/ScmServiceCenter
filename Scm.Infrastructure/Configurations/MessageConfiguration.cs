using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.AtUtc).IsDescending();

        builder.Property(x => x.Text)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.AtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(x => x.Order)
            .WithMany(o => o.Messages)
            .HasForeignKey(x => x.OrderId);
    }
}
