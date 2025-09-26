namespace Scm.Domain.Entities;

public class ReportParameterDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "string";

    public string? DefaultValue { get; set; }
}
