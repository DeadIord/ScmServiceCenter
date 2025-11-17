using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IReportBuilderService
{
    IReadOnlyCollection<ReportParameterDefinition> DeserializeParameters(string? json);

    string SerializeParameters(IEnumerable<ReportParameterDefinition> parameters);

    IReadOnlyCollection<string> DeserializeRoles(string? json);

    string SerializeRoles(IEnumerable<string> roles);

    void ValidateSqlSafety(string sql, IEnumerable<string> allowedSchemas);

    ReportQueryDefinition? DeserializeQueryDefinition(string? json);

    string SerializeQueryDefinition(ReportQueryDefinition definition);

    string GenerateSqlFromDefinition(ReportQueryDefinition definition);

    string ResolveSqlText(ReportDefinition report);

    Task<ReportDatabaseMetadata> GetDatabaseMetadataAsync(CancellationToken cancellationToken = default);

    Task<ReportExecutionResult> ExecuteAsync(ReportDefinition report, IDictionary<string, string?> parameterValues, bool isPreview, CancellationToken cancellationToken = default);
}

public class ReportExecutionResult
{
    public List<string> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public int TotalRows { get; set; }

    public string? Error { get; set; }
}
