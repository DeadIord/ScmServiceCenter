using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Scm.Web.Controllers;
using Scm.Web.Authorization;
using Scm.Web.Security;
using Xunit;

namespace Scm.Web.Tests;

public class AuthorizationPolicyTests
{
    [Fact]
    public void StockController_UsesStockAccessPolicy()
    {
        var attribute = typeof(StockController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(AuthorizationPolicies.StockAccess, attribute!.Policy);
    }

    [Fact]
    public void ReportsController_UsesReportsAccessPolicy()
    {
        var attribute = typeof(ReportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(AuthorizationPolicies.ReportsAccess, attribute!.Policy);
    }

    [Fact]
    public async Task StockPolicy_DeniesUserWithoutRequiredRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.StockAccess, policy =>
                policy.RequireRole(AuthorizationPolicies.s_stockAccessRoles));
        });
        var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Technician") }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var ret = await authorizationService.AuthorizeAsync(user, null, AuthorizationPolicies.StockAccess);

        Assert.False(ret.Succeeded);
    }

    [Fact]
    public async Task ReportsPolicy_DeniesUserWithoutRequiredRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.ReportsAccess, policy =>
                policy.RequireRole(AuthorizationPolicies.s_reportsAccessRoles));
        });
        var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Technician") }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var ret = await authorizationService.AuthorizeAsync(user, null, AuthorizationPolicies.ReportsAccess);

        Assert.False(ret.Succeeded);
    }

    [Fact]
    public async Task OrdersPolicy_AllowsClientRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.OrdersAccess, policy =>
                policy.RequireRole("Admin", "Manager", "Technician", "Client"));
        });
        var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Client") }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var ret = await authorizationService.AuthorizeAsync(user, null, PolicyNames.OrdersAccess);

        Assert.True(ret.Succeeded);
    }

    [Fact]
    public async Task MessagesPolicy_AllowsClientRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.MessagesAccess, policy =>
                policy.RequireRole("Admin", "Manager", "Technician", "Support", "Client"));
        });
        var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Client") }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var ret = await authorizationService.AuthorizeAsync(user, null, PolicyNames.MessagesAccess);

        Assert.True(ret.Succeeded);
    }
}
