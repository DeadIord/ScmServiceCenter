using System.Collections.Generic;

namespace Scm.Application.Services;

public class ReportQueryDefinition
{
    public List<ReportQueryTableDefinition> Tables { get; set; } = new();

    public List<ReportQueryColumnDefinition> Columns { get; set; } = new();

    public List<ReportQueryFilterDefinition> Filters { get; set; } = new();

    public List<ReportQuerySortDefinition> Sorts { get; set; } = new();
}

public class ReportQueryTableDefinition
{
    public string Schema { get; set; } = "public";

    public string Name { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public string JoinType { get; set; } = "Inner";

    public string? JoinCondition { get; set; }
}

public class ReportQueryColumnDefinition
{
    public string TableAlias { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public string? Alias { get; set; }

    public string? Aggregate { get; set; }

    public bool GroupBy { get; set; }

    public string? SortDirection { get; set; }

    public int? SortOrder { get; set; }
}

public class ReportQueryFilterDefinition
{
    public string Connector { get; set; } = "AND";

    public string TableAlias { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public string Operator { get; set; } = "=";

    public string? Value { get; set; }

    public string? ParameterName { get; set; }
}

public class ReportQuerySortDefinition
{
    public string TableAlias { get; set; } = string.Empty;

    public string ColumnName { get; set; } = string.Empty;

    public string Direction { get; set; } = "ASC";

    public int? Order { get; set; }
}

public class ReportDatabaseMetadata
{
    public List<ReportTableMetadata> Tables { get; set; } = new();
}

public class ReportTableMetadata
{
    public string Schema { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<ReportColumnMetadata> Columns { get; set; } = new();
}

public class ReportColumnMetadata
{
    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public bool IsNullable { get; set; }
}
