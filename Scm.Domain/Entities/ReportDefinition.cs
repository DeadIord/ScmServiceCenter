namespace Scm.Domain.Entities;

public class ReportDefinition
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SqlText { get; set; } = string.Empty;

    public string BuilderConfigurationJson { get; set; } = "{}";

    public string ParametersJson { get; set; } = "[]";

    public ReportVisibility Visibility { get; set; } = ReportVisibility.Private;

    public string AllowedRolesJson { get; set; } = "[]";

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public ICollection<ReportExecutionLog> ExecutionLogs { get; set; } = new List<ReportExecutionLog>();
}
