namespace Scm.Web.Models.Reports;

public sealed class DashboardViewModel
{
    public DateOnly? PeriodStart { get; init; }

    public DateOnly? PeriodEnd { get; init; }

    public double AverageRepairDays { get; init; }

    public double SlaViolationRate { get; init; }

    public decimal Revenue { get; init; }

    public int TotalOrders { get; init; }

    public IReadOnlyList<DashboardTopDefectRow> TopDefects { get; init; }
        = Array.Empty<DashboardTopDefectRow>();

    public IReadOnlyList<DashboardOrdersPoint> OrdersByDay { get; init; }
        = Array.Empty<DashboardOrdersPoint>();

    public IReadOnlyList<DashboardRevenuePoint> RevenueByWeek { get; init; }
        = Array.Empty<DashboardRevenuePoint>();
}

public sealed record DashboardTopDefectRow(string Defect, int Count);

public sealed record DashboardOrdersPoint(string Date, int Count);

public sealed record DashboardRevenuePoint(string Week, decimal Amount);
