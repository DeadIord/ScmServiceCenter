using System;
using Scm.Domain.Entities;

namespace Scm.Web.Models.Tasks;

public sealed class TaskListItemViewModel
{
    public Guid Id { get; set; }
        = Guid.Empty;

    public Guid OrderId { get; set; }
        = Guid.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    public TechnicianTaskStatus Status { get; set; } = TechnicianTaskStatus.Pending;

    public DateTime CreatedAtUtc { get; set; }
        = DateTime.UtcNow;

    public DateTime? DueDateUtc { get; set; }
        = null;

    public string Assignee { get; set; } = string.Empty;

    public string Initials { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public bool IsOverdue { get; set; }
        = false;
}
