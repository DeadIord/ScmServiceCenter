namespace Scm.Web.Models.Reports;

public sealed class DashboardViewModel
{
    public double AverageRepairDays { get; init; }

    public double SlaBreachPercent { get; init; }

    public decimal RevenueMonth { get; init; }

    public IReadOnlyDictionary<string, int> TopDefects { get; init; } = new Dictionary<string, int>();
}
