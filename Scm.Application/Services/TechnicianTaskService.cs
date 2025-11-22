using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public sealed class TechnicianTaskService(ScmDbContext in_dbContext) : ITechnicianTaskService
{
    private readonly ScmDbContext m_dbContext = in_dbContext;

    public async Task<TechnicianTask?> CreateForOrderAsync(Order in_order, CancellationToken in_cancellationToken = default)
    {
        TechnicianTask? ret = null;

        if (string.IsNullOrWhiteSpace(in_order.AssignedUserId))
        {
            return ret;
        }

        var task = new TechnicianTask
        {
            OrderId = in_order.Id,
            AssignedUserId = in_order.AssignedUserId,
            Title = $"Работа по заказу {in_order.Number}",
            Description = in_order.Defect,
            Priority = in_order.Priority,
            Status = TechnicianTaskStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            DueDateUtc = in_order.SLAUntil
        };

        m_dbContext.TechnicianTasks.Add(task);
        await m_dbContext.SaveChangesAsync(in_cancellationToken);

        ret = task;
        return ret;
    }

    public async Task<List<TechnicianTask>> GetAsync(string? in_q, TechnicianTaskStatus? in_status, CancellationToken in_cancellationToken = default)
    {
        var query = m_dbContext.TechnicianTasks
            .Include(t => t.Order)
            .Include(t => t.AssignedUser)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(in_q))
        {
            var term = in_q.Trim();
            query = query.Where(t =>
                t.Title.Contains(term) ||
                t.Description.Contains(term) ||
                (t.Order != null && t.Order.Number.Contains(term)));
        }

        if (in_status.HasValue)
        {
            query = query.Where(t => t.Status == in_status.Value);
        }

        var ret = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(in_cancellationToken);

        return ret;
    }

    public async Task SyncStatusFromOrderAsync(Order in_order, CancellationToken in_cancellationToken = default)
    {
        var tasks = await m_dbContext.TechnicianTasks
            .Where(t => t.OrderId == in_order.Id)
            .ToListAsync(in_cancellationToken);

        if (tasks.Count == 0)
        {
            return;
        }

        var mappedStatus = MapStatus(in_order.Status);

        foreach (var task in tasks)
        {
            task.Status = mappedStatus;
        }

        await m_dbContext.SaveChangesAsync(in_cancellationToken);
    }

    private TechnicianTaskStatus MapStatus(OrderStatus in_status)
    {
        TechnicianTaskStatus ret;

        ret = in_status switch
        {
            OrderStatus.Closed or OrderStatus.Ready => TechnicianTaskStatus.Completed,
            OrderStatus.InRepair or OrderStatus.Diagnosing or OrderStatus.WaitingApproval => TechnicianTaskStatus.InProgress,
            OrderStatus.Received => TechnicianTaskStatus.Pending,
            _ => TechnicianTaskStatus.Pending
        };

        return ret;
    }
}
