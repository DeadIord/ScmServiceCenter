using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Web.Authorization;
using Scm.Web.Models.Reports;

namespace Scm.Web.Controllers;

[Authorize(Policy = RolePolicies.ReportsAccess)]
public class ReportsController : Controller
{
    private readonly ScmDbContext _dbContext;

    public ReportsController(ScmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(DateOnly? periodStart, DateOnly? periodEnd)
    {
        var ordersQuery = _dbContext.Orders.AsQueryable();

        var startDate = periodStart.HasValue
            ? DateTime.SpecifyKind(periodStart.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : (DateTime?)null;

        var endDateExclusive = periodEnd.HasValue
            ? DateTime.SpecifyKind(periodEnd.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : (DateTime?)null;

        if (startDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAtUtc >= startDate.Value);
        }

        if (endDateExclusive.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAtUtc < endDateExclusive.Value);
        }

        var filteredOrders = await ordersQuery
            .Select(o => new
            {
                o.Id,
                o.Defect,
                o.Status,
                o.CreatedAtUtc,
                o.SLAUntil
            })
            .ToListAsync();

        var completedOrders = filteredOrders
            .Where(o => o.Status == OrderStatus.Ready || o.Status == OrderStatus.Closed)
            .ToList();

        var now = DateTime.UtcNow;

        var averageDays = completedOrders.Count > 0
            ? completedOrders.Average(o => (now - o.CreatedAtUtc).TotalDays)
            : 0d;

        var completedWithSla = completedOrders.Where(o => o.SLAUntil.HasValue).ToList();
        var slaViolations = completedWithSla.Count(o => o.SLAUntil!.Value < now);
        var slaViolationRate = completedWithSla.Count == 0
            ? 0d
            : (double)slaViolations / completedWithSla.Count * 100d;

        var orderIds = filteredOrders.Select(o => o.Id).ToArray();
        decimal revenue = 0m;

        if (orderIds.Length > 0)
        {
            revenue = await _dbContext.QuoteLines
                .Where(q => orderIds.Contains(q.OrderId))
                .Where(q => q.Status == QuoteLineStatus.Approved)
                .Where(q => q.Kind == QuoteLineKind.Labor || q.Kind == QuoteLineKind.Part)
                .SumAsync(q => q.Price * q.Qty);
        }

        var topDefects = filteredOrders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.Defect) ? "Не указано" : o.Defect)
            .Select(g => new DashboardTopDefectRow(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Defect)
            .Take(10)
            .ToList();

        var model = new DashboardViewModel
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AverageRepairDays = Math.Round(averageDays, 1),
            SlaViolationRate = Math.Round(slaViolationRate, 1),
            RevenueStub = revenue,
            TotalOrders = filteredOrders.Count,
            TopDefects = topDefects
        };

        return View(model);
    }
}
