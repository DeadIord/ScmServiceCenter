namespace Scm.Web.Models.Reports;

public sealed class DashboardViewModel
{
    public DateOnly? PeriodStart { get; init; }

    public DateOnly? PeriodEnd { get; init; }

    public double AverageRepairDays { get; init; }

    public double SlaViolationRate { get; init; }

    public decimal Revenue { get; init; }

    public decimal Refunded { get; init; }

    public decimal PlannedRevenue { get; init; }

    public string Currency { get; init; } = "RUB";

    public IReadOnlyCollection<string> AvailableCurrencies { get; init; } = Array.Empty<string>();

    public int TotalOrders { get; init; }

    public IReadOnlyList<Scm.Application.Services.DashboardTopDefectRow> TopDefects { get; init; }
        = Array.Empty<Scm.Application.Services.DashboardTopDefectRow>();

    public IReadOnlyList<Scm.Application.Services.DashboardOrdersPoint> OrdersByDay { get; init; }
        = Array.Empty<Scm.Application.Services.DashboardOrdersPoint>();

    public IReadOnlyList<Scm.Application.Services.DashboardRevenuePoint> RevenueByWeek { get; init; }
        = Array.Empty<Scm.Application.Services.DashboardRevenuePoint>();
}
