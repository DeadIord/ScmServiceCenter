using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scm.Application.Services;
using Scm.Web.Authorization;
using Scm.Web.Models.Reports;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.ReportsAccess)]
public class ReportsController : Controller
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(DateOnly? periodStart, DateOnly? periodEnd, string? currency)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStartValue = periodStart ?? today.AddDays(-13);
        var periodEndValue = periodEnd ?? today;

        var currencyValue = string.IsNullOrWhiteSpace(currency)
            ? _reportingService.BaseCurrency
            : currency;

        var dashboard = await _reportingService.GetDashboardAsync(periodStartValue, periodEndValue, currencyValue);

        var model = new DashboardViewModel
        {
            PeriodStart = dashboard.PeriodStart,
            PeriodEnd = dashboard.PeriodEnd,
            AverageRepairDays = dashboard.AverageRepairDays,
            SlaViolationRate = dashboard.SlaViolationRate,
            Revenue = dashboard.Revenue,
            Refunded = dashboard.Refunded,
            PlannedRevenue = dashboard.PlannedRevenue,
            Currency = dashboard.Currency,
            AvailableCurrencies = _reportingService.GetCurrencies(),
            TotalOrders = dashboard.TotalOrders,
            TopDefects = dashboard.TopDefects,
            OrdersByDay = dashboard.OrdersByDay,
            RevenueByWeek = dashboard.RevenueByWeek
        };

        return View(model);
    }
}
