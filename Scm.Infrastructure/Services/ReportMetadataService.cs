using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Scm.Application.Services;
using Scm.Infrastructure.Persistence;

namespace Scm.Infrastructure.Services;

public class ReportMetadataService : IReportMetadataService
{
    private readonly ScmDbContext m_dbContext;
    private readonly SemaphoreSlim m_cacheSemaphore = new(1, 1);
    private ReportMetadataDescriptor? m_cachedMetadata;

    public ReportMetadataService(ScmDbContext in_dbContext)
    {
        m_dbContext = in_dbContext;
    }

    public async Task<ReportMetadataDescriptor> GetMetadataAsync(CancellationToken in_cancellationToken = default)
    {
        if (m_cachedMetadata is not null)
        {
            return m_cachedMetadata;
        }

        await m_cacheSemaphore.WaitAsync(in_cancellationToken);
        try
        {
            if (m_cachedMetadata is null)
            {
                m_cachedMetadata = BuildMetadata();
            }
        }
        finally
        {
            m_cacheSemaphore.Release();
        }

        return m_cachedMetadata;
    }

    private ReportMetadataDescriptor BuildMetadata()
    {
        var entityTypes = m_dbContext.Model.GetEntityTypes()
            .Where(entity => !entity.IsOwned())
            .ToList();

        var tableList = new List<ReportTableMetadata>();
        var relationList = new List<ReportRelationMetadata>();

        foreach (var entityType in entityTypes)
        {
            var table = CreateTableMetadata(entityType);
            if (table is null)
            {
                continue;
            }

            tableList.Add(table);
            var relations = CreateRelations(entityType);
            relationList.AddRange(relations);
        }

        var schemaLookup = tableList
            .GroupBy(table => table.Schema)
            .Select(group => new ReportSchemaMetadata
            {
                Name = group.Key,
                TableKeys = group.Select(table => table.Key).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList()
            })
            .OrderBy(schema => schema.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var descriptor = new ReportMetadataDescriptor
        {
            Tables = tableList.OrderBy(table => table.Key, StringComparer.OrdinalIgnoreCase).ToList(),
            Relations = relationList
                .GroupBy(relation => relation.Id)
                .Select(group => group.First())
                .OrderBy(relation => relation.Id, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Schemas = schemaLookup
        };

        return descriptor;
    }

    private ReportTableMetadata? CreateTableMetadata(IMutableEntityType in_entityType)
    {
        var tableName = in_entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return null;
        }

        var schema = in_entityType.GetSchema() ?? "public";
        var identifier = StoreObjectIdentifier.Table(tableName, schema);
        var columns = new List<ReportColumnMetadata>();

        foreach (var property in in_entityType.GetProperties())
        {
            var columnName = property.GetColumnName(identifier);
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            var column = new ReportColumnMetadata
            {
                Name = columnName,
                DataType = property.GetColumnType() ?? string.Empty,
                IsNullable = property.IsColumnNullable()
            };
            columns.Add(column);
        }

        var table = new ReportTableMetadata
        {
            Key = $"{schema}.{tableName}",
            Schema = schema,
            Table = tableName,
            DisplayName = in_entityType.ClrType.Name,
            Columns = columns
                .OrderBy(column => column.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        return table;
    }

    private IEnumerable<ReportRelationMetadata> CreateRelations(IMutableEntityType in_entityType)
    {
        var result = new List<ReportRelationMetadata>();
        foreach (var foreignKey in in_entityType.GetForeignKeys())
        {
            var relation = CreateRelation(foreignKey);
            if (relation is not null)
            {
                result.Add(relation);
            }
        }

        return result;
    }

    private ReportRelationMetadata? CreateRelation(IMutableForeignKey in_foreignKey)
    {
        var dependentTable = in_foreignKey.DeclaringEntityType.GetTableName();
        var principalTable = in_foreignKey.PrincipalEntityType.GetTableName();
        if (string.IsNullOrWhiteSpace(dependentTable) || string.IsNullOrWhiteSpace(principalTable))
        {
            return null;
        }

        var dependentSchema = in_foreignKey.DeclaringEntityType.GetSchema() ?? "public";
        var principalSchema = in_foreignKey.PrincipalEntityType.GetSchema() ?? "public";

        var dependentIdentifier = StoreObjectIdentifier.Table(dependentTable, dependentSchema);
        var principalIdentifier = StoreObjectIdentifier.Table(principalTable, principalSchema);

        var columnPairs = new List<ReportRelationJoinColumn>();
        var propertyCount = in_foreignKey.Properties.Count;
        for (var index = 0; index < propertyCount; index++)
        {
            var dependentProperty = in_foreignKey.Properties[index];
            var principalProperty = in_foreignKey.PrincipalKey.Properties[index];
            var dependentColumn = dependentProperty.GetColumnName(dependentIdentifier);
            var principalColumn = principalProperty.GetColumnName(principalIdentifier);
            if (string.IsNullOrWhiteSpace(dependentColumn) || string.IsNullOrWhiteSpace(principalColumn))
            {
                continue;
            }

            var pair = new ReportRelationJoinColumn
            {
                FromColumn = dependentColumn,
                ToColumn = principalColumn
            };
            columnPairs.Add(pair);
        }

        if (columnPairs.Count == 0)
        {
            return null;
        }

        var joinType = in_foreignKey.IsRequired ? ReportJoinType.Inner : ReportJoinType.Left;
        var relation = new ReportRelationMetadata
        {
            Id = $"{dependentSchema}.{dependentTable}->{principalSchema}.{principalTable}:{string.Join(',', columnPairs.Select(p => p.FromColumn))}",
            FromTableKey = $"{dependentSchema}.{dependentTable}",
            ToTableKey = $"{principalSchema}.{principalTable}",
            JoinType = joinType,
            Columns = columnPairs
        };

        return relation;
    }
}
