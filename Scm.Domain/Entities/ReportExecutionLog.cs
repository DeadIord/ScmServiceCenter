namespace Scm.Domain.Entities;

public class ReportExecutionLog
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public ReportExecutionStatus Status { get; set; }

    public int RowCount { get; set; }

    public string? ErrorMessage { get; set; }

    public ReportDefinition? Report { get; set; }
}
