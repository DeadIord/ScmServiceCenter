using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Scm.Domain.Identity;
using Scm.Web.Areas.Admin.Controllers;
using Scm.Web.Areas.Admin.Models;
using Xunit;

namespace Scm.Web.Tests;

//public class UsersControllerTests
//{
//    [Fact]
//    public void UsersController_HasAdminAreaAndAuthorizeAttributes()
//    {
//        var type = typeof(UsersController);
//        var areaAttribute = type.GetCustomAttributes(typeof(AreaAttribute), true).OfType<AreaAttribute>().FirstOrDefault();
//        var authorizeAttribute = type.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType<AuthorizeAttribute>().FirstOrDefault();

//        Assert.NotNull(areaAttribute);
//        Assert.Equal("Admin", areaAttribute!.RouteValue);
//        Assert.NotNull(authorizeAttribute);
//        Assert.Equal("Admin", authorizeAttribute!.Roles);
//    }

//    [Fact]
//    public async Task Edit_AssignsRolesAndUpdatesStatus()
//    {
//        var user = new ApplicationUser
//        {
//            Id = Guid.NewGuid().ToString(),
//            UserName = "employee",
//            Email = "employee@example.com"
//        };

//        var userStore = new Mock<IUserStore<ApplicationUser>>();
//        var userManager = new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
//        userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
//        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
//        userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Manager" });
//        userManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
//        userManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
//        userManager.Setup(m => m.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
//        userManager.Setup(m => m.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset?>())).ReturnsAsync(IdentityResult.Success);

//        var roleStore = new Mock<IRoleStore<IdentityRole>>();
//        var roleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null, null, null, null);

//        var localizer = new Mock<IStringLocalizer<UsersController>>();
//        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
//        localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()]).Returns((string key, object[] args) => new LocalizedString(key, string.Format(key, args)));

//        var logger = new Mock<ILogger<UsersController>>();

//        var controller = new UsersController(userManager.Object, roleManager.Object, localizer.Object, logger.Object)
//        {
//            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
//        };

//        var model = new UserEditViewModel
//        {
//            Id = user.Id,
//            UserName = user.UserName!,
//            Email = user.Email!,
//            IsActive = true,
//            Roles = new List<UserRoleSelectionViewModel>
//            {
//                new() { RoleId = "1", RoleName = "Admin", Selected = true },
//                new() { RoleId = "2", RoleName = "Manager", Selected = false }
//            }
//        };

//        var result = await controller.Edit(model);

//        var redirect = Assert.IsType<RedirectToActionResult>(result);
//        Assert.Equal("Index", redirect.ActionName);
//        userManager.Verify(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("Manager"))), Times.Once);
//        userManager.Verify(m => m.AddToRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("Admin"))), Times.Once);
//        userManager.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
//    }
//}
