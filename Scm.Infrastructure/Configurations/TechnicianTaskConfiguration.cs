using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scm.Domain.Entities;

namespace Scm.Infrastructure.Configurations;

public class TechnicianTaskConfiguration : IEntityTypeConfiguration<TechnicianTask>
{
    public void Configure(EntityTypeBuilder<TechnicianTask> builder)
    {
        builder.ToTable("TechnicianTasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<int>();

        builder.Property(t => t.Status)
            .HasConversion<int>();

        builder.Property(t => t.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.DueDateUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.AssignedUserId);

        builder.HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.AssignedUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
