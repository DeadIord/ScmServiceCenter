using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IReportBuilderService
{
    IReadOnlyCollection<ReportParameterDefinition> DeserializeParameters(string? in_json);

    string SerializeParameters(IEnumerable<ReportParameterDefinition> in_parameters);

    IReadOnlyCollection<string> DeserializeRoles(string? in_json);

    string SerializeRoles(IEnumerable<string> in_roles);

    ReportQueryRequest DeserializeQuery(string? in_json);

    string SerializeQuery(ReportQueryRequest in_request);

    Task<ReportSqlGenerationResult> BuildSqlAsync(ReportQueryRequest in_request, CancellationToken in_cancellationToken = default);

    void ValidateSqlSafety(string in_sql, IEnumerable<string> in_allowedSchemas);

    Task<ReportExecutionResult> ExecuteAsync(ReportDefinition in_report, IDictionary<string, string?> in_parameterValues, bool in_isPreview, CancellationToken in_cancellationToken = default);
}

public class ReportExecutionResult
{
    public List<string> Columns { get; set; } = new();

    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public int TotalRows { get; set; }

    public string? Error { get; set; }
}
