namespace Scm.Web.Models.Reports;

public sealed class DashboardViewModel
{
    public DateOnly? PeriodStart { get; init; }

    public DateOnly? PeriodEnd { get; init; }

    public double AverageRepairDays { get; init; }

    public double SlaViolationRate { get; init; }

    public decimal RevenueStub { get; init; }

    public int TotalOrders { get; init; }

    public IReadOnlyList<DashboardTopDefectRow> TopDefects { get; init; }
        = Array.Empty<DashboardTopDefectRow>();
}

public sealed record DashboardTopDefectRow(string Defect, int Count);
