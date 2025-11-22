using System;
using Scm.Domain.Entities;

namespace Scm.Web.Models.Tasks;

public sealed class TasksIndexViewModel
{
    public string? Query { get; set; }
        = null;

    public TechnicianTaskStatus? Status { get; set; }
        = null;

    public List<TaskListItemViewModel> Tasks { get; set; }
        = new();

    public int TotalTasks { get; set; }
        = 0;

    public int CompletedTasks { get; set; }
        = 0;

    public int InProgressTasks { get; set; }
        = 0;

    public int PendingTasks { get; set; }
        = 0;
}
