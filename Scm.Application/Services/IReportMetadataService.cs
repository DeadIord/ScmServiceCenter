using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Scm.Application.Services;

public interface IReportMetadataService
{
    Task<ReportMetadataDescriptor> GetMetadataAsync(CancellationToken in_cancellationToken = default);
}

public class ReportMetadataDescriptor
{
    public IReadOnlyCollection<ReportSchemaMetadata> Schemas { get; set; } = Array.Empty<ReportSchemaMetadata>();

    public IReadOnlyCollection<ReportTableMetadata> Tables { get; set; } = Array.Empty<ReportTableMetadata>();

    public IReadOnlyCollection<ReportRelationMetadata> Relations { get; set; } = Array.Empty<ReportRelationMetadata>();
}

public class ReportSchemaMetadata
{
    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<string> TableKeys { get; set; } = Array.Empty<string>();
}

public class ReportTableMetadata
{
    public string Key { get; set; } = string.Empty;

    public string Schema { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<ReportColumnMetadata> Columns { get; set; } = Array.Empty<ReportColumnMetadata>();
}

public class ReportColumnMetadata
{
    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsNullable { get; set; }
        = true;
}

public class ReportRelationMetadata
{
    public string Id { get; set; } = string.Empty;

    public string FromTableKey { get; set; } = string.Empty;

    public string ToTableKey { get; set; } = string.Empty;

    public ReportJoinType JoinType { get; set; } = ReportJoinType.Inner;

    public IReadOnlyCollection<ReportRelationJoinColumn> Columns { get; set; } = Array.Empty<ReportRelationJoinColumn>();
}

public class ReportRelationJoinColumn
{
    public string FromColumn { get; set; } = string.Empty;

    public string ToColumn { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportJoinType
{
    Inner,
    Left
}

public class ReportQueryRequest
{
    public List<string> Tables { get; set; } = new();

    public List<ReportQueryFieldSelection> Fields { get; set; } = new();

    public List<string> Relations { get; set; } = new();

    public List<ReportQueryFilter> Filters { get; set; } = new();

    public bool UseDistinct { get; set; }
        = false;

    public bool AllowManualSql { get; set; }
        = false;
}

public class ReportQueryFieldSelection
{
    public string TableKey { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public string? Alias { get; set; }
        = null;
}

public class ReportQueryFilter
{
    public string TableKey { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public string Operator { get; set; } = "=";

    public string ParameterName { get; set; } = string.Empty;
}

public class ReportSqlGenerationResult
{
    public string Sql { get; set; } = string.Empty;

    public IReadOnlyCollection<string> ParameterNames { get; set; } = Array.Empty<string>();
}

public static class ReportQuerySerializationHelper
{
    private static readonly Regex AliasRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public static string Serialize(ReportQueryRequest in_request)
    {
        var ret = JsonSerializer.Serialize(in_request);
        return ret;
    }

    public static ReportQueryRequest Deserialize(string? in_json)
    {
        ReportQueryRequest ret;
        if (string.IsNullOrWhiteSpace(in_json))
        {
            ret = new ReportQueryRequest();
        }
        else
        {
            ret = JsonSerializer.Deserialize<ReportQueryRequest>(in_json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ReportQueryRequest();
        }

        Normalize(ret);
        return ret;
    }

    public static bool IsSafeAlias(string in_value)
    {
        var ret = !string.IsNullOrWhiteSpace(in_value) && AliasRegex.IsMatch(in_value);
        return ret;
    }

    private static void Normalize(ReportQueryRequest in_request)
    {
        if (in_request.Tables.Count == 0)
        {
            return;
        }

        for (var index = 0; index < in_request.Tables.Count; index++)
        {
            var tableKey = in_request.Tables[index];
            in_request.Tables[index] = tableKey?.Trim() ?? string.Empty;
        }

        foreach (var field in in_request.Fields)
        {
            field.TableKey = field.TableKey?.Trim() ?? string.Empty;
            field.ColumnName = field.ColumnName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(field.Alias) && !IsSafeAlias(field.Alias))
            {
                field.Alias = null;
            }
        }

        foreach (var relation in in_request.Relations)
        {
        }

        foreach (var filter in in_request.Filters)
        {
            filter.TableKey = filter.TableKey?.Trim() ?? string.Empty;
            filter.ColumnName = filter.ColumnName?.Trim() ?? string.Empty;
            filter.Operator = filter.Operator?.Trim().ToUpperInvariant() ?? "=";
            filter.ParameterName = filter.ParameterName?.Trim() ?? string.Empty;
        }
    }
}
