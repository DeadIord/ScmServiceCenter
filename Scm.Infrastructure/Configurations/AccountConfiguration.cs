using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Inn)
            .HasMaxLength(32);

        builder.Property(a => a.Address)
            .HasMaxLength(500);

        builder.Property(a => a.Tags)
            .HasMaxLength(256);

        builder.Property(a => a.ManagerUserId)
            .HasMaxLength(450);
    }
}
