using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Scm.Web.Models.ReportBuilder;

public class ReportQueryBuilderViewModel
{
    public List<ReportQueryTableInputModel> Tables { get; set; } = new();

    public List<ReportQueryColumnInputModel> Columns { get; set; } = new();

    public List<ReportQueryFilterInputModel> Filters { get; set; } = new();

    public List<ReportQuerySortInputModel> Sorts { get; set; } = new();
}

public class ReportQueryTableInputModel
{
    [Required]
    public string Schema { get; set; } = "public";

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Alias { get; set; }

    [Required]
    public string JoinType { get; set; } = "Inner";

    public string? JoinCondition { get; set; }
}

public class ReportQueryColumnInputModel
{
    public string TableAlias { get; set; } = string.Empty;

    [Required]
    public string ColumnName { get; set; } = string.Empty;

    public string? Alias { get; set; }

    public string? Aggregate { get; set; }

    public bool GroupBy { get; set; }

    public string? SortDirection { get; set; }

    public int? SortOrder { get; set; }
}

public class ReportQueryFilterInputModel
{
    public string Connector { get; set; } = "AND";

    public string TableAlias { get; set; } = string.Empty;

    [Required]
    public string ColumnName { get; set; } = string.Empty;

    [Required]
    public string Operator { get; set; } = "=";

    public string? Value { get; set; }

    public string? ParameterName { get; set; }
}

public class ReportQuerySortInputModel
{
    public string TableAlias { get; set; } = string.Empty;

    [Required]
    public string ColumnName { get; set; } = string.Empty;

    [Required]
    public string Direction { get; set; } = "ASC";

    public int? Order { get; set; }
}
