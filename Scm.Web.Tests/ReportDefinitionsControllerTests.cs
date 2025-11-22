using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Scm.Application.Services;
using Scm.Domain.Identity;
using Scm.Infrastructure.Persistence;
using Scm.Web.Controllers;
using Scm.Web.Models.ReportBuilder;
using Xunit;

namespace Scm.Web.Tests;

public sealed class ReportDefinitionsControllerTests : IDisposable
{
    private readonly ScmDbContext _dbContext;
    private readonly ReportBuilderService _reportBuilderService;
    private readonly ReportDefinitionsController _controller;

    public ReportDefinitionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ScmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ScmDbContext(options);
        _reportBuilderService = new ReportBuilderService(_dbContext);

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null!, null!, null!, null!);

        _controller = new ReportDefinitionsController(_dbContext, _reportBuilderService, userManager.Object, roleManager.Object);
    }

    [Fact]
    public void GenerateSql_WithValidBuilder_ReturnsSqlPreview()
    {
        var builderModel = new ReportQueryBuilderViewModel
        {
            Tables = new List<ReportQueryTableInputModel>
            {
                new()
                {
                    Schema = "public",
                    Name = "Orders",
                    Alias = "o"
                }
            },
            Columns = new List<ReportQueryColumnInputModel>
            {
                new()
                {
                    TableAlias = "o",
                    ColumnName = "Number",
                    Alias = "Номер"
                }
            }
        };

        var result = _controller.GenerateSql(builderModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var property = okResult.Value?.GetType().GetProperty("sql");
        Assert.NotNull(property);
        var sql = property?.GetValue(okResult.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("\"Orders\"", sql!, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _controller.Dispose();
        _dbContext.Dispose();
    }
}
