using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class ReportExecutionLogConfiguration : IEntityTypeConfiguration<ReportExecutionLog>
{
    public void Configure(EntityTypeBuilder<ReportExecutionLog> builder)
    {
        builder.ToTable("ReportExecutionLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.FinishedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ReportId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Report)
            .WithMany(r => r.ExecutionLogs)
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
