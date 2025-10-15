using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Application.Services;

public class ReportBuilderService : IReportBuilderService
{
    private static readonly Regex ForbiddenCommandsRegex = new("\\b(INSERT|UPDATE|DELETE|ALTER|DROP|CREATE|TRUNCATE)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SchemaRegex = new("\\b([a-zA-Z_][\\w]*)\\.", RegexOptions.Compiled);

    private readonly ScmDbContext _dbContext;

    public ReportBuilderService(ScmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyCollection<ReportParameterDefinition> DeserializeParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ReportParameterDefinition>();
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<ReportParameterDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result ?? new List<ReportParameterDefinition>();
        }
        catch
        {
            return Array.Empty<ReportParameterDefinition>();
        }
    }

    public string SerializeParameters(IEnumerable<ReportParameterDefinition> parameters)
    {
        return JsonSerializer.Serialize(parameters);
    }

    public IReadOnlyCollection<string> DeserializeRoles(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        try
        {
            var result = JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public string SerializeRoles(IEnumerable<string> roles)
    {
        var normalized = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    public void ValidateSqlSafety(string sql, IEnumerable<string> allowedSchemas)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("SQL-запрос не может быть пустым.");
        }

        var trimmed = sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Разрешены только SELECT-запросы.");
        }

        if (ForbiddenCommandsRegex.IsMatch(trimmed))
        {
            throw new InvalidOperationException("Запрос содержит запрещённые команды SQL.");
        }

        var allowedSet = new HashSet<string>(allowedSchemas.Select(s => s.ToLowerInvariant()));
        foreach (Match match in SchemaRegex.Matches(trimmed))
        {
            var schema = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(schema) && !allowedSet.Contains(schema.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"Схема '{schema}' не входит в белый список.");
            }
        }
    }

    public async Task<ReportExecutionResult> ExecuteAsync(ReportDefinition report, IDictionary<string, string?> parameterValues, bool isPreview, CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = PrepareSql(report.SqlText, isPreview);
        command.CommandType = CommandType.Text;

        var parameters = DeserializeParameters(report.ParametersJson);
        foreach (var parameter in parameters)
        {
            var value = parameterValues.TryGetValue(parameter.Name, out var provided) ? provided : parameter.DefaultValue;
            command.Parameters.Add(CreateParameter(command, parameter, value));
        }

        var result = new ReportExecutionResult();

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                foreach (var column in result.Columns)
                {
                    var value = reader[column];
                    row[column] = value == DBNull.Value ? null : value;
                }

                result.Rows.Add(row);
            }

            result.TotalRows = result.Rows.Count;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return result;
    }

    private static DbParameter CreateParameter(DbCommand command, ReportParameterDefinition definition, string? value)
    {
        var parameterName = definition.Name.StartsWith("@", StringComparison.Ordinal) ? definition.Name : "@" + definition.Name;
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;

        switch (definition.Type.ToLowerInvariant())
        {
            case "int":
            case "integer":
                parameter.DbType = DbType.Int32;
                parameter.Value = int.TryParse(value, out var intValue) ? intValue : (object)DBNull.Value;
                break;
            case "decimal":
            case "numeric":
                parameter.DbType = DbType.Decimal;
                parameter.Value = decimal.TryParse(value, out var decimalValue) ? decimalValue : (object)DBNull.Value;
                break;
            case "date":
                parameter.DbType = DbType.Date;
                parameter.Value = DateTime.TryParse(value, out var dateValue) ? dateValue.Date : (object)DBNull.Value;
                break;
            case "bool":
            case "boolean":
                parameter.DbType = DbType.Boolean;
                parameter.Value = bool.TryParse(value, out var boolValue) ? boolValue : (object)DBNull.Value;
                break;
            default:
                parameter.DbType = DbType.String;
                parameter.Value = value ?? (object)DBNull.Value;
                break;
        }

        return parameter;
    }

    private static string PrepareSql(string originalSql, bool isPreview)
    {
        var sql = originalSql.Trim().TrimEnd(';');
        var limit = isPreview ? 200 : 50000;

        if (!Regex.IsMatch(sql, "\\bLIMIT\\b", RegexOptions.IgnoreCase))
        {
            sql = $"{sql} LIMIT {limit}";
        }
        else if (isPreview)
        {
            sql = $"SELECT * FROM ({sql}) AS preview_subquery LIMIT {limit}";
        }

        return sql;
    }
}
