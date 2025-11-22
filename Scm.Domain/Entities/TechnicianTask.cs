using System;
using Scm.Domain.Identity;

namespace Scm.Domain.Entities;

public enum TechnicianTaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class TechnicianTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }
        = Guid.Empty;

    public string? AssignedUserId { get; set; }
        = null;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    public TechnicianTaskStatus Status { get; set; } = TechnicianTaskStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DueDateUtc { get; set; }
        = null;

    public Order? Order { get; set; }
        = null;

    public ApplicationUser? AssignedUser { get; set; }
        = null;
}
