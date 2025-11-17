using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Web.Authorization;
using Scm.Web.Models.Reports;
using Scm.Web.Security;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.ReportsAccess)]
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStartValue = periodStart ?? today.AddDays(-13);
        var periodEndValue = periodEnd ?? today;

        var startDate = DateTime.SpecifyKind(periodStartValue.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateExclusive = DateTime.SpecifyKind(periodEndValue.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var ordersQuery = _dbContext.Orders
            .Where(o => o.CreatedAtUtc >= startDate)
            .Where(o => o.CreatedAtUtc < endDateExclusive);

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
        var orderCreatedLookup = filteredOrders.ToDictionary(o => o.Id, o => o.CreatedAtUtc);

        var approvedLines = orderIds.Length > 0
            ? await _dbContext.QuoteLines
                .Where(q => orderIds.Contains(q.OrderId))
                .Where(q => q.Status == QuoteLineStatus.Approved)
                .Where(q => q.Kind == QuoteLineKind.Labor || q.Kind == QuoteLineKind.Part)
                .Select(q => new QuoteLinePoint(q.OrderId, q.Price, q.Qty))
                .ToListAsync()
            : new List<QuoteLinePoint>();

        var revenue = approvedLines.Sum(q => q.Price * q.Qty);

        var ordersByDate = filteredOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAtUtc.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var ordersTimeline = new List<DashboardOrdersPoint>();
        for (var date = periodStartValue; date <= periodEndValue; date = date.AddDays(1))
        {
            ordersTimeline.Add(new DashboardOrdersPoint(date.ToString("dd.MM"), ordersByDate.GetValueOrDefault(date)));
        }

        var weeklyRevenue = approvedLines
            .GroupBy(line => GetWeekStart(DateOnly.FromDateTime(orderCreatedLookup[line.OrderId].Date)))
            .OrderBy(g => g.Key)
            .Select(group => new DashboardRevenuePoint(GetWeekLabel(group.Key), group.Sum(q => q.Price * q.Qty)))
            .ToList();

        var topDefects = filteredOrders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.Defect) ? "Не указано" : o.Defect)
            .Select(g => new DashboardTopDefectRow(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Defect)
            .Take(10)
            .ToList();

        var model = new DashboardViewModel
        {
            PeriodStart = periodStartValue,
            PeriodEnd = periodEndValue,
            AverageRepairDays = Math.Round(averageDays, 1),
            SlaViolationRate = Math.Round(slaViolationRate, 1),
            Revenue = revenue,
            TotalOrders = filteredOrders.Count,
            TopDefects = topDefects,
            OrdersByDay = ordersTimeline,
            RevenueByWeek = weeklyRevenue
        };

        return View(model);
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.AddDays(-offset);
    }

    private static string GetWeekLabel(DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return $"{weekStart:dd.MM} - {weekEnd:dd.MM}";
    }

    private sealed record QuoteLinePoint(Guid OrderId, decimal Price, decimal Qty);
}
