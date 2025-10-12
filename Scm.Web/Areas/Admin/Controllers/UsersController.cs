using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Scm.Infrastructure.Identity;
using Scm.Web.Areas.Admin.Models;

namespace Scm.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> m_userManager;
    private readonly RoleManager<IdentityRole> m_roleManager;
    private readonly IStringLocalizer<UsersController> m_localizer;
    private readonly ILogger<UsersController> m_logger;

    public UsersController(
        UserManager<ApplicationUser> in_userManager,
        RoleManager<IdentityRole> in_roleManager,
        IStringLocalizer<UsersController> in_localizer,
        ILogger<UsersController> in_logger)
    {
        m_userManager = in_userManager;
        m_roleManager = in_roleManager;
        m_localizer = in_localizer;
        m_logger = in_logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IActionResult ret;
        var users = await m_userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var model = new UserListViewModel();
        foreach (var user in users)
        {
            var roles = await m_userManager.GetRolesAsync(user);
            var isLocked = await m_userManager.IsLockedOutAsync(user);
            var item = new UserListItemViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles,
                IsActive = !isLocked
            };
            model.Users.Add(item);
        }

        ret = View(model);
        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        IActionResult ret;
        var model = new UserCreateViewModel
        {
            IsActive = true
        };

        await PopulateRolesAsync(model);
        ret = View(model);
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel in_model)
    {
        IActionResult ret = View(in_model);
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync(in_model);
        }
        else
        {
            var user = new ApplicationUser
            {
                DisplayName = in_model.DisplayName,
                Email = in_model.Email,
                PhoneNumber = in_model.PhoneNumber,
                UserName = in_model.UserName
            };

            var createResult = await m_userManager.CreateAsync(user, in_model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await PopulateRolesAsync(in_model);
            }
            else
            {
                try
                {
                    await SynchronizeUserRolesAsync(user, in_model);
                    await UpdateLockoutAsync(user, in_model.IsActive);
                    TempData["Success"] = m_localizer["Notification_UserCreated", user.UserName].Value;
                    ret = RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Failed to finalize user creation for {UserId}", user.Id);
                    TempData["Error"] = m_localizer["Error_CreateFailed"].Value;
                    ModelState.AddModelError(string.Empty, m_localizer["Error_CreateFailed"].Value);
                    await PopulateRolesAsync(in_model);
                }
            }
        }

        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string in_id)
    {
        IActionResult ret;
        var user = await m_userManager.FindByIdAsync(in_id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            var isLocked = await m_userManager.IsLockedOutAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName ?? string.Empty,
                IsActive = !isLocked
            };
            await PopulateRolesAsync(model, await m_userManager.GetRolesAsync(user));
            ret = View(model);
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel in_model)
    {
        IActionResult ret = View(in_model);
        var user = await m_userManager.FindByIdAsync(in_model.Id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            if (!ModelState.IsValid)
            {
                await PopulateRolesAsync(in_model);
            }
            else
            {
                user.DisplayName = in_model.DisplayName;
                user.Email = in_model.Email;
                user.PhoneNumber = in_model.PhoneNumber;
                user.UserName = in_model.UserName;

                var updateResult = await m_userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await PopulateRolesAsync(in_model);
                }
                else
                {
                    try
                    {
                        await SynchronizeUserRolesAsync(user, in_model);
                        await UpdateLockoutAsync(user, in_model.IsActive);
                        TempData["Success"] = m_localizer["Notification_UserUpdated", user.UserName].Value;
                        ret = RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        m_logger.LogError(ex, "Failed to update roles for user {UserId}", user.Id);
                        TempData["Error"] = m_localizer["Error_UpdateFailed"].Value;
                        ModelState.AddModelError(string.Empty, m_localizer["Error_UpdateFailed"].Value);
                        await PopulateRolesAsync(in_model);
                    }
                }
            }
        }

        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string in_id)
    {
        IActionResult ret;
        var user = await m_userManager.FindByIdAsync(in_id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            var model = new UserResetPasswordViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName ?? user.UserName
            };
            ret = View(model);
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel in_model)
    {
        IActionResult ret = View(in_model);
        var user = await m_userManager.FindByIdAsync(in_model.Id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            if (ModelState.IsValid)
            {
                var token = await m_userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await m_userManager.ResetPasswordAsync(user, token, in_model.Password);
                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    TempData["Success"] = m_localizer["Notification_PasswordReset", user.UserName].Value;
                    ret = RedirectToAction(nameof(Index));
                }
            }
            else
            {
                TempData["Error"] = m_localizer["Error_PasswordValidation"].Value;
            }
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string in_id, bool in_isActive)
    {
        IActionResult ret = RedirectToAction(nameof(Index));
        var user = await m_userManager.FindByIdAsync(in_id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
        }
        else
        {
            try
            {
                await UpdateLockoutAsync(user, in_isActive);
                TempData["Success"] = m_localizer["Notification_StatusUpdated", user.UserName].Value;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to update status for user {UserId}", in_id);
                TempData["Error"] = m_localizer["Error_StatusUpdateFailed"].Value;
            }
        }

        return ret;
    }

    private async Task PopulateRolesAsync(UserEditViewModel in_model, IList<string>? in_selectedRoles = null)
    {
        var selectedList = in_selectedRoles ?? in_model.Roles
            .Where(r => r.Selected)
            .Select(r => r.RoleName)
            .ToList();
        var selectedRoles = new HashSet<string>(selectedList.Where(r => !string.IsNullOrWhiteSpace(r)), StringComparer.OrdinalIgnoreCase);

        var roles = await m_roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        var items = new List<UserRoleSelectionViewModel>();
        foreach (var role in roles)
        {
            var name = role.Name ?? string.Empty;
            var item = new UserRoleSelectionViewModel
            {
                RoleId = role.Id,
                RoleName = name,
                Selected = selectedRoles.Contains(name)
            };
            items.Add(item);
        }

        in_model.Roles = items;
    }

    private async Task SynchronizeUserRolesAsync(ApplicationUser in_user, UserEditViewModel in_model)
    {
        var desiredRoles = in_model.Roles
            .Where(r => r.Selected)
            .Select(r => r.RoleName)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();

        var currentRoles = await m_userManager.GetRolesAsync(in_user);
        var toRemove = currentRoles.Except(desiredRoles).ToList();
        if (toRemove.Count > 0)
        {
            var removeResult = await m_userManager.RemoveFromRolesAsync(in_user, toRemove);
            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        var toAdd = desiredRoles.Except(currentRoles).ToList();
        if (toAdd.Count > 0)
        {
            var addResult = await m_userManager.AddToRolesAsync(in_user, toAdd);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task UpdateLockoutAsync(ApplicationUser in_user, bool in_isActive)
    {
        var enableResult = await m_userManager.SetLockoutEnabledAsync(in_user, true);
        if (!enableResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", enableResult.Errors.Select(e => e.Description)));
        }
        IdentityResult ret;
        if (in_isActive)
        {
            ret = await m_userManager.SetLockoutEndDateAsync(in_user, null);
        }
        else
        {
            ret = await m_userManager.SetLockoutEndDateAsync(in_user, DateTimeOffset.UtcNow.AddYears(100));
        }

        if (!ret.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", ret.Errors.Select(e => e.Description)));
        }
    }
}
