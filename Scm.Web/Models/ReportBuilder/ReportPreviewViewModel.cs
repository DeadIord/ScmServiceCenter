using Scm.Domain.Entities;

namespace Scm.Web.Models.ReportBuilder;

public class ReportPreviewViewModel
{
    public Guid ReportId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<ReportParameterInputModel> Parameters { get; set; } = new();

    public Dictionary<string, string?> Values { get; set; } = new();

    public List<string> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public string? Error { get; set; }

    public bool HasResults => Rows.Count > 0;
}
