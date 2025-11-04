using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Xunit;

namespace Scm.Web.Tests;

public sealed class ReportBuilderServiceTests : IDisposable
{
    private readonly ScmDbContext _dbContext;
    private readonly ReportBuilderService _service;

    public ReportBuilderServiceTests()
    {
        var options = new DbContextOptionsBuilder<ScmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ScmDbContext(options);
        _service = new ReportBuilderService(_dbContext);
    }

    [Fact]
    public void GenerateSqlFromDefinition_WithJoinAndFilters_BuildsSelectQuery()
    {
        var definition = new ReportQueryDefinition
        {
            Tables = new List<ReportQueryTableDefinition>
            {
                new()
                {
                    Schema = "public",
                    Name = "Orders",
                    Alias = "o"
                },
                new()
                {
                    Schema = "public",
                    Name = "QuoteLines",
                    Alias = "q",
                    JoinType = "Left",
                    JoinCondition = "o.\"Id\" = q.\"OrderId\""
                }
            },
            Columns = new List<ReportQueryColumnDefinition>
            {
                new()
                {
                    TableAlias = "o",
                    ColumnName = "Number",
                    Alias = "OrderNumber",
                    GroupBy = true
                },
                new()
                {
                    TableAlias = "q",
                    ColumnName = "Price",
                    Aggregate = "SUM",
                    Alias = "TotalAmount"
                }
            },
            Filters = new List<ReportQueryFilterDefinition>
            {
                new()
                {
                    TableAlias = "o",
                    ColumnName = "Status",
                    Operator = "=",
                    ParameterName = "@status"
                }
            },
            Sorts = new List<ReportQuerySortDefinition>
            {
                new()
                {
                    TableAlias = "o",
                    ColumnName = "Number",
                    Direction = "ASC",
                    Order = 1
                }
            }
        };

        var sql = _service.GenerateSqlFromDefinition(definition);

        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"Orders\"", sql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN \"public\".\"QuoteLines\"", sql, StringComparison.Ordinal);
        Assert.Contains("SUM", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@status", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSqlText_WithLegacyDefinition_ReturnsStoredSql()
    {
        var report = new ReportDefinition
        {
            SqlText = "SELECT * FROM \"Orders\"",
            QueryDefinitionJson = "{}"
        };

        var sql = _service.ResolveSqlText(report);

        Assert.Equal(report.SqlText, sql);
    }

    [Fact]
    public void ResolveSqlText_WithStructuredDefinition_ReturnsGeneratedSql()
    {
        var definition = new ReportQueryDefinition
        {
            Tables = new List<ReportQueryTableDefinition>
            {
                new()
                {
                    Schema = "public",
                    Name = "Orders",
                    Alias = "o"
                }
            },
            Columns = new List<ReportQueryColumnDefinition>
            {
                new()
                {
                    TableAlias = "o",
                    ColumnName = "Number",
                    Alias = "Number"
                }
            }
        };

        var report = new ReportDefinition
        {
            SqlText = "SELECT 1",
            QueryDefinitionJson = _service.SerializeQueryDefinition(definition)
        };

        var sql = _service.ResolveSqlText(report);

        Assert.Contains("\"Orders\"", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT 1", sql, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
