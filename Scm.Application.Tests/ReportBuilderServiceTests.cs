using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Infrastructure.Services;
using Xunit;

namespace Scm.Application.Tests;

public class ReportBuilderServiceTests
{
    private static ScmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ScmDbContext(options);
        return context;
    }

    private static ReportBuilderService CreateService(ScmDbContext in_context)
    {
        var metadataService = new ReportMetadataService(in_context);
        var service = new ReportBuilderService(in_context, metadataService);
        return service;
    }

    [Fact]
    public async Task BuildSqlAsync_ReturnsSelectForSingleTable()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var request = new ReportQueryRequest
        {
            Tables = ["public.Orders"],
            Fields =
            [
                new ReportQueryFieldSelection
                {
                    TableKey = "public.Orders",
                    ColumnName = "Number"
                }
            ]
        };

        var result = await service.BuildSqlAsync(request);

        Assert.Contains("FROM \"public\".\"Orders\"", result.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("t0.\"Number\"", result.Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildSqlAsync_AppliesRelationJoin()
    {
        using var context = CreateContext();
        var metadataService = new ReportMetadataService(context);
        var metadata = await metadataService.GetMetadataAsync();
        var relation = metadata.Relations.First(relation => relation.FromTableKey == "public.Orders" && relation.ToTableKey == "public.Accounts");
        var service = new ReportBuilderService(context, metadataService);

        var request = new ReportQueryRequest
        {
            Tables = ["public.Orders", "public.Accounts"],
            Fields =
            [
                new ReportQueryFieldSelection
                {
                    TableKey = "public.Orders",
                    ColumnName = "Number"
                },
                new ReportQueryFieldSelection
                {
                    TableKey = "public.Accounts",
                    ColumnName = "Name"
                }
            ],
            Relations = [relation.Id]
        };

        var result = await service.BuildSqlAsync(request);

        Assert.Contains("JOIN \"public\".\"Accounts\"", result.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("t0.\"Number\"", result.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("t1.\"Name\"", result.Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildSqlAsync_ThrowsForUnknownColumn()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var request = new ReportQueryRequest
        {
            Tables = ["public.Orders"],
            Fields =
            [
                new ReportQueryFieldSelection
                {
                    TableKey = "public.Orders",
                    ColumnName = "Missing"
                }
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BuildSqlAsync(request));
    }
}
