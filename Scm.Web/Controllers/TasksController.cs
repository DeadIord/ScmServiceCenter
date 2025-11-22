using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scm.Web.Authorization;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Domain.Identity;
using Scm.Web.Models.Tasks;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.CrmAccess)]
public sealed class TasksController : Controller
{
    private readonly ILogger<TasksController> m_logger;
    private readonly ITechnicianTaskService m_taskService;

    public TasksController(
        ILogger<TasksController> in_logger,
        ITechnicianTaskService in_taskService)
    {
        m_logger = in_logger;
        m_taskService = in_taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? in_q, TechnicianTaskStatus? in_status)
    {
        m_logger.LogInformation("Открыт раздел задач.");
        var allTasks = await m_taskService.GetAsync(null, null);
        var filteredTasks = await m_taskService.GetAsync(in_q, in_status);

        var items = filteredTasks
            .Select(task => MapTask(task))
            .ToList();

        var model = new TasksIndexViewModel
        {
            Query = in_q,
            Status = in_status,
            Tasks = items,
            TotalTasks = allTasks.Count,
            CompletedTasks = allTasks.Count(t => t.Status == TechnicianTaskStatus.Completed),
            InProgressTasks = allTasks.Count(t => t.Status == TechnicianTaskStatus.InProgress),
            PendingTasks = allTasks.Count(t => t.Status == TechnicianTaskStatus.Pending)
        };

        IActionResult ret = View(model);
        return ret;
    }

    private TaskListItemViewModel MapTask(TechnicianTask in_task)
    {
        var now = DateTime.UtcNow;
        var assigneeName = GetUserDisplayName(in_task.AssignedUser);

        var ret = new TaskListItemViewModel
        {
            Id = in_task.Id,
            OrderId = in_task.OrderId,
            Title = in_task.Title,
            Description = in_task.Description,
            Priority = in_task.Priority,
            Status = in_task.Status,
            CreatedAtUtc = in_task.CreatedAtUtc,
            DueDateUtc = in_task.DueDateUtc,
            Assignee = assigneeName,
            Initials = BuildInitials(assigneeName),
            OrderNumber = in_task.Order?.Number ?? string.Empty,
            IsOverdue = in_task.DueDateUtc.HasValue && in_task.DueDateUtc.Value < now && in_task.Status != TechnicianTaskStatus.Completed
        };

        return ret;
    }

    private string BuildInitials(string in_value)
    {
        if (string.IsNullOrWhiteSpace(in_value))
        {
            return "?";
        }

        var words = in_value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "?";
        }

        var letters = words.Take(2).Select(w => w[0]);
        var ret = string.Concat(letters).ToUpperInvariant();
        return ret;
    }

    private string GetUserDisplayName(ApplicationUser? in_user)
    {
        if (in_user is null)
        {
            return "Не назначен";
        }

        if (!string.IsNullOrWhiteSpace(in_user.DisplayName))
        {
            return in_user.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(in_user.UserName))
        {
            return in_user.UserName;
        }

        return in_user.Email ?? "Не назначен";
    }
}