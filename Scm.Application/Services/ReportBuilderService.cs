using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
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

    public ReportQueryDefinition? DeserializeQueryDefinition(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ReportQueryDefinition>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    public string SerializeQueryDefinition(ReportQueryDefinition definition)
    {
        return JsonSerializer.Serialize(definition);
    }

    public string GenerateSqlFromDefinition(ReportQueryDefinition definition)
    {
        if (definition.Tables.Count == 0)
        {
            return string.Empty;
        }

        var aliasMap = BuildAliasMap(definition.Tables);
        var selectExpressions = BuildSelectExpressions(definition.Columns, aliasMap);
        var builder = new StringBuilder();
        builder.Append("SELECT ");

        if (selectExpressions.Count == 0)
        {
            var firstAlias = aliasMap.Values.FirstOrDefault();
            builder.Append(string.IsNullOrWhiteSpace(firstAlias) ? "*" : $"{firstAlias}.*");
        }
        else
        {
            builder.Append(string.Join(", ", selectExpressions));
        }

        builder.Append(" FROM ");
        builder.Append(BuildTableSource(definition.Tables[0]));

        for (var i = 1; i < definition.Tables.Count; i++)
        {
            builder.Append(' ');
            builder.Append(BuildJoinClause(definition.Tables[i]));
        }

        var filters = BuildFilterClauses(definition.Filters, aliasMap);
        if (filters.Count > 0)
        {
            builder.Append(" WHERE ");
            builder.Append(string.Join(" ", filters));
        }

        var groupBy = BuildGroupBy(definition.Columns, aliasMap);
        if (groupBy.Count > 0)
        {
            builder.Append(" GROUP BY ");
            builder.Append(string.Join(", ", groupBy));
        }

        var orderBy = BuildOrderBy(definition, aliasMap);
        if (orderBy.Count > 0)
        {
            builder.Append(" ORDER BY ");
            builder.Append(string.Join(", ", orderBy));
        }

        return builder.ToString();
    }

    public string ResolveSqlText(ReportDefinition report)
    {
        var definition = DeserializeQueryDefinition(report.QueryDefinitionJson);
        if (definition is null)
        {
            return report.SqlText;
        }

        var sql = GenerateSqlFromDefinition(definition);
        return string.IsNullOrWhiteSpace(sql) ? report.SqlText : sql;
    }

    public async Task<ReportDatabaseMetadata> GetDatabaseMetadataAsync(CancellationToken cancellationToken = default)
    {
        const string metadataSql = "SELECT table_schema, table_name, column_name, data_type, is_nullable, ordinal_position FROM information_schema.columns WHERE table_schema NOT IN ('pg_catalog', 'information_schema') ORDER BY table_schema, table_name, ordinal_position";

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var tables = new Dictionary<string, ReportTableMetadata>(StringComparer.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = metadataSql;
        command.CommandType = CommandType.Text;

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var schema = reader.GetString(0);
                var tableName = reader.GetString(1);
                var columnName = reader.GetString(2);
                var dataType = reader.GetString(3);
                var isNullable = string.Equals(reader.GetString(4), "YES", StringComparison.OrdinalIgnoreCase);

                var key = $"{schema}.{tableName}";
                if (!tables.TryGetValue(key, out var table))
                {
                    table = new ReportTableMetadata
                    {
                        Schema = schema,
                        Name = tableName,
                        Columns = new List<ReportColumnMetadata>()
                    };
                    tables[key] = table;
                }

                table.Columns.Add(new ReportColumnMetadata
                {
                    Name = columnName,
                    DataType = dataType,
                    IsNullable = isNullable
                });
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        var ordered = tables.Values
            .OrderBy(t => t.Schema, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        foreach (var table in ordered)
        {
            table.Columns = table.Columns
                .OrderBy(c => c.Name, StringComparer.Ordinal)
                .ToList();
        }

        return new ReportDatabaseMetadata
        {
            Tables = ordered
        };
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
        var sql = ResolveSqlText(report);
        command.CommandText = PrepareSql(sql, isPreview);
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

    private static Dictionary<string, string> BuildAliasMap(IEnumerable<ReportQueryTableDefinition> tables)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
            {
                continue;
            }

            var alias = string.IsNullOrWhiteSpace(table.Alias) ? table.Name : table.Alias;
            var formattedAlias = string.IsNullOrWhiteSpace(alias) ? string.Empty : EscapeIdentifier(alias);

            if (!string.IsNullOrWhiteSpace(alias) && !map.ContainsKey(alias))
            {
                map.Add(alias, formattedAlias);
            }

            if (!map.ContainsKey(table.Name))
            {
                map.Add(table.Name, formattedAlias);
            }
        }

        return map;
    }

    private static List<string> BuildSelectExpressions(IEnumerable<ReportQueryColumnDefinition> columns, IDictionary<string, string> aliasMap)
    {
        var expressions = new List<string>();

        foreach (var column in columns)
        {
            if (string.IsNullOrWhiteSpace(column.ColumnName))
            {
                continue;
            }

            var source = BuildQualifiedIdentifier(column.TableAlias, column.ColumnName, aliasMap);
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var expression = source;
            if (!string.IsNullOrWhiteSpace(column.Aggregate))
            {
                expression = $"{column.Aggregate.Trim().ToUpperInvariant()}({source})";
            }

            if (!string.IsNullOrWhiteSpace(column.Alias))
            {
                expression += $" AS {EscapeIdentifier(column.Alias)}";
            }

            expressions.Add(expression);
        }

        return expressions;
    }

    private static string BuildTableSource(ReportQueryTableDefinition table)
    {
        var builder = new StringBuilder();
        builder.Append($"{EscapeIdentifier(table.Schema)}.{EscapeIdentifier(table.Name)}");
        if (!string.IsNullOrWhiteSpace(table.Alias))
        {
            builder.Append(' ');
            builder.Append(EscapeIdentifier(table.Alias));
        }

        return builder.ToString();
    }

    private static string BuildJoinClause(ReportQueryTableDefinition table)
    {
        var joinType = table.JoinType?.Trim().ToUpperInvariant() switch
        {
            "LEFT" => "LEFT JOIN",
            "RIGHT" => "RIGHT JOIN",
            "FULL" => "FULL JOIN",
            "CROSS" => "CROSS JOIN",
            _ => "INNER JOIN"
        };

        var builder = new StringBuilder();
        builder.Append(joinType);
        builder.Append(' ');
        builder.Append($"{EscapeIdentifier(table.Schema)}.{EscapeIdentifier(table.Name)}");

        if (!string.IsNullOrWhiteSpace(table.Alias))
        {
            builder.Append(' ');
            builder.Append(EscapeIdentifier(table.Alias));
        }

        if (!string.IsNullOrWhiteSpace(table.JoinCondition))
        {
            builder.Append(" ON ");
            builder.Append(table.JoinCondition);
        }

        return builder.ToString();
    }

    private static List<string> BuildFilterClauses(IEnumerable<ReportQueryFilterDefinition> filters, IDictionary<string, string> aliasMap)
    {
        var clauses = new List<string>();
        var isFirst = true;

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.ColumnName))
            {
                continue;
            }

            var left = BuildQualifiedIdentifier(filter.TableAlias, filter.ColumnName, aliasMap);
            if (string.IsNullOrWhiteSpace(left))
            {
                continue;
            }

            var operatorText = string.IsNullOrWhiteSpace(filter.Operator) ? "=" : filter.Operator.Trim().ToUpperInvariant();
            string clause;

            if (operatorText is "IS NULL" or "IS NOT NULL")
            {
                clause = $"{left} {operatorText}";
            }
            else
            {
                var right = BuildFilterRightHand(filter, operatorText);
                if (string.IsNullOrWhiteSpace(right))
                {
                    continue;
                }

                if (operatorText is "IN" or "NOT IN")
                {
                    clause = $"{left} {operatorText} ({right})";
                }
                else if (operatorText == "BETWEEN")
                {
                    clause = $"{left} BETWEEN {right}";
                }
                else
                {
                    clause = $"{left} {operatorText} {right}";
                }
            }

            if (!isFirst)
            {
                var connector = string.IsNullOrWhiteSpace(filter.Connector) ? "AND" : filter.Connector.Trim().ToUpperInvariant();
                clauses.Add(connector);
            }
            else
            {
                isFirst = false;
            }

            clauses.Add(clause);
        }

        return clauses;
    }

    private static List<string> BuildGroupBy(IEnumerable<ReportQueryColumnDefinition> columns, IDictionary<string, string> aliasMap)
    {
        return columns
            .Where(column => column.GroupBy && !string.IsNullOrWhiteSpace(column.ColumnName))
            .Select(column => BuildQualifiedIdentifier(column.TableAlias, column.ColumnName, aliasMap))
            .Where(expression => !string.IsNullOrWhiteSpace(expression))
            .Distinct()
            .ToList();
    }

    private static List<string> BuildOrderBy(ReportQueryDefinition definition, IDictionary<string, string> aliasMap)
    {
        var orderItems = new List<(int Order, string Expression)>();

        foreach (var column in definition.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.SortDirection))
            {
                continue;
            }

            var source = BuildQualifiedIdentifier(column.TableAlias, column.ColumnName, aliasMap);
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            var direction = column.SortDirection.Trim().Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            var order = column.SortOrder ?? int.MaxValue - 1;
            orderItems.Add((order, $"{source} {direction}"));
        }

        foreach (var sort in definition.Sorts)
        {
            if (string.IsNullOrWhiteSpace(sort.ColumnName))
            {
                continue;
            }

            string expression;
            if (string.IsNullOrWhiteSpace(sort.TableAlias))
            {
                expression = EscapeIdentifier(sort.ColumnName);
            }
            else
            {
                expression = BuildQualifiedIdentifier(sort.TableAlias, sort.ColumnName, aliasMap);
            }

            if (string.IsNullOrWhiteSpace(expression))
            {
                continue;
            }

            var direction = sort.Direction.Trim().Equals("DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            var order = sort.Order ?? int.MaxValue;
            orderItems.Add((order, $"{expression} {direction}"));
        }

        return orderItems
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Expression, StringComparer.Ordinal)
            .Select(item => item.Expression)
            .ToList();
    }

    private static string BuildQualifiedIdentifier(string tableAlias, string columnName, IDictionary<string, string> aliasMap)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return string.Empty;
        }

        var column = EscapeIdentifier(columnName);

        if (aliasMap.Count == 0)
        {
            return column;
        }

        var effectiveAlias = tableAlias;
        if (string.IsNullOrWhiteSpace(effectiveAlias))
        {
            effectiveAlias = aliasMap.Keys.First();
        }

        if (!aliasMap.TryGetValue(effectiveAlias, out var resolvedAlias) || string.IsNullOrWhiteSpace(resolvedAlias))
        {
            resolvedAlias = EscapeIdentifier(effectiveAlias);
        }

        if (string.IsNullOrWhiteSpace(resolvedAlias))
        {
            return column;
        }

        return $"{resolvedAlias}.{column}";
    }

    private static string BuildFilterRightHand(ReportQueryFilterDefinition filter, string operatorText)
    {
        if (!string.IsNullOrWhiteSpace(filter.ParameterName))
        {
            return NormalizeParameterName(filter.ParameterName);
        }

        if (string.IsNullOrWhiteSpace(filter.Value))
        {
            return string.Empty;
        }

        if (operatorText is "IN" or "NOT IN")
        {
            var values = filter.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => FormatLiteral(part.Trim()))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            return values.Count == 0 ? string.Empty : string.Join(", ", values);
        }

        if (operatorText == "BETWEEN")
        {
            var parts = filter.Value
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => FormatLiteral(part.Trim()))
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            return parts.Count == 2 ? string.Join(" AND ", parts) : string.Empty;
        }

        return FormatLiteral(filter.Value);
    }

    private static string NormalizeParameterName(string parameterName)
    {
        var trimmed = parameterName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.StartsWith("@", StringComparison.Ordinal) ? trimmed : "@" + trimmed;
    }

    private static string FormatLiteral(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return "NULL";
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue ? "TRUE" : "FALSE";
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue.ToString(CultureInfo.InvariantCulture);
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue.ToString(CultureInfo.InvariantCulture);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateValue) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
        {
            return $"'{dateValue:yyyy-MM-dd HH:mm:ss}'";
        }

        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }

    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return string.Empty;
        }

        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
