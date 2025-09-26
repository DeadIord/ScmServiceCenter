using Scm.Domain.Entities;

namespace Scm.Web.Models.ReportBuilder;

public class ReportDefinitionListItemViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ReportVisibility Visibility { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public bool CanEdit { get; set; }
}
