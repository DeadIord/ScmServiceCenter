using System.Data;
using System.Data.Common;
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
    private static readonly Regex ParameterNameRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private readonly ScmDbContext m_dbContext;
    private readonly IReportMetadataService m_metadataService;

    public ReportBuilderService(ScmDbContext in_dbContext, IReportMetadataService in_metadataService)
    {
        m_dbContext = in_dbContext;
        m_metadataService = in_metadataService;
    }

    public IReadOnlyCollection<ReportParameterDefinition> DeserializeParameters(string? in_json)
    {
        IReadOnlyCollection<ReportParameterDefinition> ret;
        if (string.IsNullOrWhiteSpace(in_json))
        {
            ret = Array.Empty<ReportParameterDefinition>();
        }
        else
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<ReportParameterDefinition>>(in_json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                ret = list ?? new List<ReportParameterDefinition>();
            }
            catch
            {
                ret = Array.Empty<ReportParameterDefinition>();
            }
        }

        return ret;
    }

    public string SerializeParameters(IEnumerable<ReportParameterDefinition> in_parameters)
    {
        var ret = JsonSerializer.Serialize(in_parameters);
        return ret;
    }

    public IReadOnlyCollection<string> DeserializeRoles(string? in_json)
    {
        IReadOnlyCollection<string> ret;
        if (string.IsNullOrWhiteSpace(in_json))
        {
            ret = Array.Empty<string>();
        }
        else
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(in_json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<string>();
                var normalized = list
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Select(role => role.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                ret = normalized;
            }
            catch
            {
                ret = Array.Empty<string>();
            }
        }

        return ret;
    }

    public string SerializeRoles(IEnumerable<string> in_roles)
    {
        var normalized = in_roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ret = JsonSerializer.Serialize(normalized);
        return ret;
    }

    public ReportQueryRequest DeserializeQuery(string? in_json)
    {
        var ret = ReportQuerySerializationHelper.Deserialize(in_json);
        return ret;
    }

    public string SerializeQuery(ReportQueryRequest in_request)
    {
        var ret = ReportQuerySerializationHelper.Serialize(in_request);
        return ret;
    }

    public async Task<ReportSqlGenerationResult> BuildSqlAsync(ReportQueryRequest in_request, CancellationToken in_cancellationToken = default)
    {
        if (in_request is null)
        {
            throw new ArgumentNullException(nameof(in_request));
        }

        var normalized = ReportQuerySerializationHelper.Deserialize(ReportQuerySerializationHelper.Serialize(in_request));
        var metadata = await m_metadataService.GetMetadataAsync(in_cancellationToken);
        var ret = BuildSqlInternal(normalized, metadata);
        return ret;
    }

    public void ValidateSqlSafety(string in_sql, IEnumerable<string> in_allowedSchemas)
    {
        if (string.IsNullOrWhiteSpace(in_sql))
        {
            throw new InvalidOperationException("SQL-запрос не может быть пустым.");
        }

        var trimmed = in_sql.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Разрешены только SELECT-запросы.");
        }

        if (ForbiddenCommandsRegex.IsMatch(trimmed))
        {
            throw new InvalidOperationException("Запрос содержит запрещённые команды SQL.");
        }

        var allowedSet = new HashSet<string>(in_allowedSchemas.Select(schema => schema.ToLowerInvariant()));
        foreach (Match match in SchemaRegex.Matches(trimmed))
        {
            var schema = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(schema) && !allowedSet.Contains(schema.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"Схема '{schema}' не входит в белый список.");
            }
        }
    }

    public async Task<ReportExecutionResult> ExecuteAsync(ReportDefinition in_report, IDictionary<string, string?> in_parameterValues, bool in_isPreview, CancellationToken in_cancellationToken = default)
    {
        if (in_report is null)
        {
            throw new ArgumentNullException(nameof(in_report));
        }

        if (in_parameterValues is null)
        {
            throw new ArgumentNullException(nameof(in_parameterValues));
        }

        var connection = m_dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(in_cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = PrepareSql(in_report.SqlText, in_isPreview);
        command.CommandType = CommandType.Text;

        var parameters = DeserializeParameters(in_report.ParametersJson);
        foreach (var parameter in parameters)
        {
            var value = in_parameterValues.TryGetValue(parameter.Name, out var provided) ? provided : parameter.DefaultValue;
            command.Parameters.Add(CreateParameter(command, parameter, value));
        }

        var ret = new ReportExecutionResult();

        try
        {
            await using var reader = await command.ExecuteReaderAsync(in_cancellationToken);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                ret.Columns.Add(reader.GetName(index));
            }

            var hasRows = true;
            while (hasRows)
            {
                var readResult = await reader.ReadAsync(in_cancellationToken);
                if (!readResult)
                {
                    hasRows = false;
                }
                else
                {
                    var row = new Dictionary<string, object?>();
                    foreach (var column in ret.Columns)
                    {
                        var value = reader[column];
                        if (value == DBNull.Value)
                        {
                            row[column] = null;
                        }
                        else
                        {
                            row[column] = value;
                        }
                    }

                    ret.Rows.Add(row);
                }
            }

            ret.TotalRows = ret.Rows.Count;
        }
        catch (Exception ex)
        {
            ret.Error = ex.Message;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return ret;
    }

    private ReportSqlGenerationResult BuildSqlInternal(ReportQueryRequest in_request, ReportMetadataDescriptor in_metadata)
    {
        if (in_request.Tables.Count == 0)
        {
            throw new InvalidOperationException("Выберите хотя бы одну таблицу.");
        }

        var tableLookup = in_metadata.Tables.ToDictionary(table => table.Key, table => table, StringComparer.OrdinalIgnoreCase);
        foreach (var tableKey in in_request.Tables)
        {
            if (!tableLookup.ContainsKey(tableKey))
            {
                throw new InvalidOperationException($"Таблица '{tableKey}' не разрешена для отчётов.");
            }
        }

        var aliasLookup = CreateAliasLookup(in_request.Tables);
        var selectClause = BuildSelectClause(in_request, tableLookup, aliasLookup);
        var fromClause = BuildFromClause(in_request.Tables[0], tableLookup, aliasLookup);
        var joinClauses = BuildJoinClauses(in_request, tableLookup, aliasLookup, in_metadata.Relations);
        var filterResult = BuildWhereClause(in_request.Filters, tableLookup, aliasLookup);

        var builder = new StringBuilder();
        builder.Append("SELECT ");
        if (in_request.UseDistinct)
        {
            builder.Append("DISTINCT ");
        }

        builder.AppendLine(selectClause);
        builder.AppendLine(fromClause);
        foreach (var joinClause in joinClauses)
        {
            builder.AppendLine(joinClause);
        }

        if (filterResult.Conditions.Count > 0)
        {
            builder.Append("WHERE ");
            builder.AppendLine(string.Join(" AND ", filterResult.Conditions));
        }

        var ret = new ReportSqlGenerationResult
        {
            Sql = builder.ToString().Trim(),
            ParameterNames = filterResult.ParameterNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList()
        };
        return ret;
    }

    private static Dictionary<string, string> CreateAliasLookup(IEnumerable<string> in_tables)
    {
        var ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;
        foreach (var table in in_tables)
        {
            if (!ret.ContainsKey(table))
            {
                ret[table] = $"t{index}";
                index++;
            }
        }

        return ret;
    }

    private string BuildSelectClause(ReportQueryRequest in_request, IDictionary<string, ReportTableMetadata> in_tables, IDictionary<string, string> in_aliases)
    {
        var selections = new List<string>();
        foreach (var field in in_request.Fields)
        {
            if (!in_tables.TryGetValue(field.TableKey, out var table))
            {
                throw new InvalidOperationException($"Таблица '{field.TableKey}' недоступна для выбора.");
            }

            var column = table.Columns.FirstOrDefault(col => string.Equals(col.Name, field.ColumnName, StringComparison.OrdinalIgnoreCase));
            if (column is null)
            {
                throw new InvalidOperationException($"Столбец '{field.ColumnName}' недоступен в таблице '{field.TableKey}'.");
            }

            var alias = in_aliases[field.TableKey];
            var expression = $"{alias}.\"{column.Name}\"";
            if (!string.IsNullOrWhiteSpace(field.Alias) && ReportQuerySerializationHelper.IsSafeAlias(field.Alias))
            {
                expression += $" AS \"{field.Alias}\"";
            }

            selections.Add(expression);
        }

        if (selections.Count == 0)
        {
            var firstAlias = in_aliases[in_request.Tables[0]];
            selections.Add($"{firstAlias}.*");
        }

        var ret = string.Join(", ", selections);
        return ret;
    }

    private string BuildFromClause(string in_baseTable, IDictionary<string, ReportTableMetadata> in_tables, IDictionary<string, string> in_aliases)
    {
        var table = in_tables[in_baseTable];
        var identifier = FormatTableIdentifier(table.Schema, table.Table);
        var alias = in_aliases[in_baseTable];
        var ret = $"FROM {identifier} {alias}";
        return ret;
    }

    private List<string> BuildJoinClauses(ReportQueryRequest in_request, IDictionary<string, ReportTableMetadata> in_tables, IDictionary<string, string> in_aliases, IReadOnlyCollection<ReportRelationMetadata> in_relations)
    {
        var ret = new List<string>();
        var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            in_request.Tables[0]
        };
        var relationLookup = in_relations.ToDictionary(relation => relation.Id, relation => relation, StringComparer.OrdinalIgnoreCase);
        var remaining = new Queue<string>(in_request.Relations);
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (remaining.Count > 0)
        {
            var relationId = remaining.Dequeue();
            var isProcessed = processed.Contains(relationId);
            if (!isProcessed)
            {
                processed.Add(relationId);
                if (relationLookup.TryGetValue(relationId, out var relation))
                {
                    var leftIncluded = included.Contains(relation.FromTableKey);
                    var rightIncluded = included.Contains(relation.ToTableKey);
                    if (leftIncluded == rightIncluded)
                    {
                        if (!leftIncluded)
                        {
                            remaining.Enqueue(relationId);
                        }
                    }
                    else
                    {
                        var joinTarget = leftIncluded ? relation.ToTableKey : relation.FromTableKey;
                        if (in_tables.ContainsKey(joinTarget))
                        {
                            var joinAlias = in_aliases[joinTarget];
                            var sourceTable = leftIncluded ? relation.FromTableKey : relation.ToTableKey;
                            var sourceAlias = in_aliases[sourceTable];
                            var joinType = relation.JoinType == ReportJoinType.Left ? "LEFT JOIN" : "INNER JOIN";
                            var joinIdentifier = FormatTableIdentifier(in_tables[joinTarget].Schema, in_tables[joinTarget].Table);
                            var conditions = new List<string>();

                            foreach (var pair in relation.Columns)
                            {
                                if (leftIncluded)
                                {
                                    conditions.Add($"{sourceAlias}.\"{pair.FromColumn}\" = {joinAlias}.\"{pair.ToColumn}\"");
                                }
                                else
                                {
                                    conditions.Add($"{sourceAlias}.\"{pair.ToColumn}\" = {joinAlias}.\"{pair.FromColumn}\"");
                                }
                            }

                            var joinClause = $"{joinType} {joinIdentifier} {joinAlias} ON {string.Join(" AND ", conditions)}";
                            ret.Add(joinClause);
                            included.Add(joinTarget);
                        }
                    }
                }
            }
        }

        foreach (var tableKey in in_request.Tables)
        {
            if (!included.Contains(tableKey))
            {
                throw new InvalidOperationException($"Для таблицы '{tableKey}' не указаны корректные связи.");
            }
        }

        return ret;
    }

    private (List<string> Conditions, HashSet<string> ParameterNames) BuildWhereClause(IEnumerable<ReportQueryFilter> in_filters, IDictionary<string, ReportTableMetadata> in_tables, IDictionary<string, string> in_aliases)
    {
        var conditions = new List<string>();
        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowedOperators = new HashSet<string>(new[] { "=", "<>", ">", "<", ">=", "<=", "LIKE", "NOT LIKE", "ILIKE", "NOT ILIKE" }, StringComparer.OrdinalIgnoreCase);

        foreach (var filter in in_filters)
        {
            if (!in_tables.TryGetValue(filter.TableKey, out var table))
            {
                throw new InvalidOperationException($"Таблица '{filter.TableKey}' недоступна для фильтрации.");
            }

            var column = table.Columns.FirstOrDefault(col => string.Equals(col.Name, filter.ColumnName, StringComparison.OrdinalIgnoreCase));
            if (column is null)
            {
                throw new InvalidOperationException($"Столбец '{filter.ColumnName}' недоступен в таблице '{filter.TableKey}'.");
            }

            if (!allowedOperators.Contains(filter.Operator))
            {
                throw new InvalidOperationException($"Оператор '{filter.Operator}' не поддерживается.");
            }

            if (string.IsNullOrWhiteSpace(filter.ParameterName) || !ParameterNameRegex.IsMatch(filter.ParameterName))
            {
                throw new InvalidOperationException("Для фильтров требуется корректное имя параметра.");
            }

            var alias = in_aliases[filter.TableKey];
            var condition = $"{alias}.\"{column.Name}\" {filter.Operator} @{filter.ParameterName}";
            conditions.Add(condition);
            parameterNames.Add(filter.ParameterName);
        }

        return (conditions, parameterNames);
    }

    private static string FormatTableIdentifier(string in_schema, string in_table)
    {
        var schema = string.IsNullOrWhiteSpace(in_schema) ? "public" : in_schema;
        var ret = $"\"{schema}\".\"{in_table}\"";
        return ret;
    }

    private static DbParameter CreateParameter(DbCommand in_command, ReportParameterDefinition in_definition, string? in_value)
    {
        var parameterName = in_definition.Name.StartsWith("@", StringComparison.Ordinal) ? in_definition.Name : "@" + in_definition.Name;
        var parameter = in_command.CreateParameter();
        parameter.ParameterName = parameterName;

        var type = in_definition.Type.ToLowerInvariant();
        if (type == "int" || type == "integer")
        {
            parameter.DbType = DbType.Int32;
            if (int.TryParse(in_value, out var intValue))
            {
                parameter.Value = intValue;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
        else if (type == "decimal" || type == "numeric")
        {
            parameter.DbType = DbType.Decimal;
            if (decimal.TryParse(in_value, out var decimalValue))
            {
                parameter.Value = decimalValue;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
        else if (type == "date")
        {
            parameter.DbType = DbType.Date;
            if (DateTime.TryParse(in_value, out var dateValue))
            {
                parameter.Value = dateValue.Date;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
        else if (type == "bool" || type == "boolean")
        {
            parameter.DbType = DbType.Boolean;
            if (bool.TryParse(in_value, out var boolValue))
            {
                parameter.Value = boolValue;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }
        else
        {
            parameter.DbType = DbType.String;
            if (in_value is null)
            {
                parameter.Value = DBNull.Value;
            }
            else
            {
                parameter.Value = in_value;
            }
        }

        return parameter;
    }

    private static string PrepareSql(string in_originalSql, bool in_isPreview)
    {
        var sql = in_originalSql.Trim().TrimEnd(';');
        var limit = in_isPreview ? 200 : 50000;
        if (!Regex.IsMatch(sql, "\\bLIMIT\\b", RegexOptions.IgnoreCase))
        {
            sql = $"{sql} LIMIT {limit}";
        }
        else if (in_isPreview)
        {
            sql = $"SELECT * FROM ({sql}) AS preview_subquery LIMIT {limit}";
        }

        return sql;
    }
}
