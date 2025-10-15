using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("ReportDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.SqlText)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ParametersJson)
            .HasColumnType("text")
            .HasDefaultValue("[]");

        builder.Property(x => x.AllowedRolesJson)
            .HasColumnType("text")
            .HasDefaultValue("[]");

        builder.Property(x => x.Visibility)
            .HasConversion<int>();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Visibility);
        builder.HasIndex(x => x.CreatedBy);
    }
}
