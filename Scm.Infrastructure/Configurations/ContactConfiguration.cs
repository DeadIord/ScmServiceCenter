using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.Position)
            .HasMaxLength(128);

        builder.Property(c => c.Phone)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne(c => c.Account)
            .WithMany(a => a.Contacts)
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
