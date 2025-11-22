using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public interface IReportingService
{
    string BaseCurrency { get; }

    IReadOnlyCollection<string> GetCurrencies();

    Task<DashboardReportResult> GetDashboardAsync(
        DateOnly in_periodStart,
        DateOnly in_periodEnd,
        string? in_currency = null,
        CancellationToken in_cancellationToken = default);
}

public sealed class ReportingService : IReportingService
{
    private readonly ScmDbContext _dbContext;
    private readonly IMoneyConverter _moneyConverter;

    public ReportingService(ScmDbContext in_dbContext, IMoneyConverter in_moneyConverter)
    {
        _dbContext = in_dbContext;
        _moneyConverter = in_moneyConverter;
    }

    public string BaseCurrency => _moneyConverter.BaseCurrency;

    public IReadOnlyCollection<string> GetCurrencies()
    {
        var ret = _moneyConverter.GetCurrencies();
        return ret;
    }

    public async Task<DashboardReportResult> GetDashboardAsync(
        DateOnly in_periodStart,
        DateOnly in_periodEnd,
        string? in_currency = null,
        CancellationToken in_cancellationToken = default)
    {
        var targetCurrency = _moneyConverter.NormalizeCurrency(in_currency);
        var startDate = DateTime.SpecifyKind(in_periodStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateExclusive = DateTime.SpecifyKind(in_periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

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
            .ToListAsync(in_cancellationToken);

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

        var approvedLines = orderIds.Length > 0
            ? await _dbContext.QuoteLines
                .Where(q => orderIds.Contains(q.OrderId))
                .Where(q => q.Status == QuoteLineStatus.Approved)
                .Where(q => q.Kind == QuoteLineKind.Labor || q.Kind == QuoteLineKind.Part)
                .Select(q => new QuoteLinePoint(q.OrderId, q.Price, q.Qty))
                .ToListAsync(in_cancellationToken)
            : new List<QuoteLinePoint>();

        var plannedRevenue = _moneyConverter.Sum(
            approvedLines.Select(l => new Money(l.Price * l.Qty, _moneyConverter.BaseCurrency)),
            targetCurrency);

        var invoices = orderIds.Length > 0
            ? await _dbContext.Invoices
                .Where(i => orderIds.Contains(i.OrderId))
                .ToListAsync(in_cancellationToken)
            : new List<Invoice>();

        var paidTotal = _moneyConverter.Sum(
            invoices.Where(i => i.Status == InvoiceStatus.Paid)
                .Select(i => new Money(i.Amount, i.Currency)),
            targetCurrency);

        var refundedTotal = _moneyConverter.Sum(
            invoices.Where(i => i.Status == InvoiceStatus.Refunded)
                .Select(i => new Money(i.Amount, i.Currency)),
            targetCurrency);

        var revenueAmount = paidTotal.Amount - refundedTotal.Amount;
        var revenue = new Money(revenueAmount, targetCurrency);

        var ordersByDate = filteredOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAtUtc.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var ordersTimeline = new List<DashboardOrdersPoint>();
        for (var date = in_periodStart; date <= in_periodEnd; date = date.AddDays(1))
        {
            ordersTimeline.Add(new DashboardOrdersPoint(date.ToString("dd.MM"), ordersByDate.GetValueOrDefault(date)));
        }

        var weeklyRevenueAccumulator = new Dictionary<DateOnly, decimal>();
        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Paid))
        {
            var paidDate = invoice.PaidAt ?? invoice.CreatedAt;
            var weekStart = GetWeekStart(DateOnly.FromDateTime(paidDate.Date));
            var converted = _moneyConverter.Convert(new Money(invoice.Amount, invoice.Currency), targetCurrency);
            weeklyRevenueAccumulator[weekStart] = weeklyRevenueAccumulator.GetValueOrDefault(weekStart) + converted.Amount;
        }

        foreach (var refund in invoices.Where(i => i.Status == InvoiceStatus.Refunded))
        {
            var refundDate = refund.PaidAt ?? refund.CreatedAt;
            var weekStart = GetWeekStart(DateOnly.FromDateTime(refundDate.Date));
            var converted = _moneyConverter.Convert(new Money(refund.Amount, refund.Currency), targetCurrency);
            weeklyRevenueAccumulator[weekStart] = weeklyRevenueAccumulator.GetValueOrDefault(weekStart) - converted.Amount;
        }

        var weeklyRevenue = weeklyRevenueAccumulator
            .OrderBy(g => g.Key)
            .Select(group => new DashboardRevenuePoint(GetWeekLabel(group.Key), Math.Round(group.Value, 2)))
            .ToList();

        var topDefects = filteredOrders
            .GroupBy(o => string.IsNullOrWhiteSpace(o.Defect) ? "Не указано" : o.Defect)
            .Select(g => new DashboardTopDefectRow(g.Key, g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Defect)
            .Take(10)
            .ToList();

        var ret = new DashboardReportResult
        {
            PeriodStart = in_periodStart,
            PeriodEnd = in_periodEnd,
            AverageRepairDays = Math.Round(averageDays, 1),
            SlaViolationRate = Math.Round(slaViolationRate, 1),
            Revenue = revenue.Amount,
            Refunded = refundedTotal.Amount,
            PlannedRevenue = plannedRevenue.Amount,
            Currency = targetCurrency,
            TotalOrders = filteredOrders.Count,
            TopDefects = topDefects,
            OrdersByDay = ordersTimeline,
            RevenueByWeek = weeklyRevenue
        };

        return ret;
    }

    private static DateOnly GetWeekStart(DateOnly in_date)
    {
        var dayOfWeek = (int)in_date.DayOfWeek;
        var offset = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var ret = in_date.AddDays(-offset);
        return ret;
    }

    private static string GetWeekLabel(DateOnly in_weekStart)
    {
        var weekEnd = in_weekStart.AddDays(6);
        var ret = $"{in_weekStart:dd.MM} - {weekEnd:dd.MM}";
        return ret;
    }

    private sealed record QuoteLinePoint(Guid OrderId, decimal Price, decimal Qty);
}

public sealed class DashboardReportResult
{
    public DateOnly PeriodStart { get; init; }

    public DateOnly PeriodEnd { get; init; }

    public double AverageRepairDays { get; init; }

    public double SlaViolationRate { get; init; }

    public decimal Revenue { get; init; }

    public decimal Refunded { get; init; }

    public decimal PlannedRevenue { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int TotalOrders { get; init; }

    public IReadOnlyList<DashboardTopDefectRow> TopDefects { get; init; } = Array.Empty<DashboardTopDefectRow>();

    public IReadOnlyList<DashboardOrdersPoint> OrdersByDay { get; init; } = Array.Empty<DashboardOrdersPoint>();

    public IReadOnlyList<DashboardRevenuePoint> RevenueByWeek { get; init; } = Array.Empty<DashboardRevenuePoint>();
}

public sealed record DashboardTopDefectRow(string Defect, int Count);

public sealed record DashboardOrdersPoint(string Date, int Count);

public sealed record DashboardRevenuePoint(string Week, decimal Amount);
