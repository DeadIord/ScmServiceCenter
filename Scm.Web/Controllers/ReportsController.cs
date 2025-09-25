using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Web.Models.Reports;

namespace Scm.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ScmDbContext _dbContext;

    public ReportsController(ScmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var now = DateTime.UtcNow;
        var completed = await _dbContext.Orders.Where(o => o.Status == OrderStatus.Ready || o.Status == OrderStatus.Closed).ToListAsync();
        var averageDays = completed.Any() ? completed.Average(o => (now - o.CreatedAtUtc).TotalDays) : 0;

        var totalOrders = await _dbContext.Orders.CountAsync();
        var slaBreaches = await _dbContext.Orders.CountAsync(o => o.SLAUntil != null && o.SLAUntil < now && o.Status != OrderStatus.Closed);
        var slaPercent = totalOrders == 0 ? 0 : (double)slaBreaches / totalOrders * 100d;

        var revenue = await _dbContext.Invoices.Where(i => i.CreatedAt.Month == now.Month && i.CreatedAt.Year == now.Year)
            .SumAsync(i => i.Amount);

        var topDefects = await _dbContext.Orders
            .GroupBy(o => o.Defect)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var model = new DashboardViewModel
        {
            AverageRepairDays = Math.Round(averageDays, 1),
            SlaBreachPercent = Math.Round(slaPercent, 1),
            RevenueMonth = revenue,
            TopDefects = topDefects
        };

        return View(model);
    }
}
