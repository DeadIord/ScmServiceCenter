using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Scm.Infrastructure.Identity;
using Scm.Web.Areas.Admin.Models.Users;
using Scm.Web.Authorization;

namespace Scm.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = PolicyNames.AdministrationAccess)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> m_userManager;
    private readonly RoleManager<IdentityRole> m_roleManager;
    private readonly ILogger<UsersController> m_logger;
    private readonly IStringLocalizer<UsersController> m_localizer;
    private readonly SignInManager<ApplicationUser> m_signInManager;

    public UsersController(
        UserManager<ApplicationUser> in_userManager,
        RoleManager<IdentityRole> in_roleManager,
        ILogger<UsersController> in_logger,
        IStringLocalizer<UsersController> in_localizer,
        SignInManager<ApplicationUser> in_signInManager)
    {
        m_userManager = in_userManager;
        m_roleManager = in_roleManager;
        m_logger = in_logger;
        m_localizer = in_localizer;
        m_signInManager = in_signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? query)
    {
        IActionResult ret;
        IQueryable<ApplicationUser> usersQuery = m_userManager.Users;
        if (!string.IsNullOrWhiteSpace(query))
        {
            usersQuery = usersQuery.Where(u =>
                (u.Email != null && EF.Functions.ILike(u.Email, $"%{query}%")) ||
                (u.DisplayName != null && EF.Functions.ILike(u.DisplayName, $"%{query}%")));
        }

        List<ApplicationUser> users = await usersQuery
            .OrderBy(u => u.DisplayName ?? u.Email ?? string.Empty)
            .ToListAsync();

        List<UserListItemViewModel> items = new();
        foreach (ApplicationUser user in users)
        {
            IList<string> roles = await m_userManager.GetRolesAsync(user);
            UserListItemViewModel item = new()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow,
                Roles = roles
            };
            items.Add(item);
        }

        UserIndexViewModel model = new()
        {
            Query = query,
            Users = items
        };

        ret = View(model);
        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        IActionResult ret;
        UserCreateViewModel model = new()
        {
            Roles = await buildRoleOptionsAsync(null)
        };
        ret = View(model);
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        IActionResult ret;
        if (!ModelState.IsValid)
        {
            model.Roles = await buildRoleOptionsAsync(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList());
            ret = View(model);
            return ret;
        }

        ApplicationUser user = new()
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = true
        };

        IdentityResult createResult = await m_userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (IdentityError error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = await buildRoleOptionsAsync(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList());
            ret = View(model);
            return ret;
        }

        List<string> selectedRoles = model.Roles
            .Where(r => r.IsSelected)
            .Select(r => r.RoleName)
            .ToList();

        IdentityResult lockResult = await applyLockoutAsync(user, model.IsLocked);
        if (!lockResult.Succeeded)
        {
            foreach (IdentityError error in lockResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = await buildRoleOptionsAsync(selectedRoles);
            await m_userManager.DeleteAsync(user);
            ret = View(model);
            return ret;
        }

        if (selectedRoles.Count > 0)
        {
            IdentityResult roleResult = await m_userManager.AddToRolesAsync(user, selectedRoles);
            if (!roleResult.Succeeded)
            {
                foreach (IdentityError error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                model.Roles = await buildRoleOptionsAsync(selectedRoles);
                await m_userManager.DeleteAsync(user);
                ret = View(model);
                return ret;
            }
        }

        TempData["Success"] = m_localizer["Notification_UserCreated", user.Email ?? string.Empty].Value;
        ret = RedirectToAction(nameof(Index));
        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        IActionResult ret;
        ApplicationUser? user = await m_userManager.FindByIdAsync(id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        IList<string> userRoles = await m_userManager.GetRolesAsync(user);
        UserEditViewModel model = new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            PhoneNumber = user.PhoneNumber,
            IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow,
            Roles = await buildRoleOptionsAsync(userRoles)
        };

        ret = View(model);
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        IActionResult ret;
        if (!ModelState.IsValid)
        {
            model.Roles = await buildRoleOptionsAsync(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList());
            ret = View(model);
            return ret;
        }

        ApplicationUser? user = await m_userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        user.DisplayName = model.DisplayName;
        user.PhoneNumber = model.PhoneNumber;
        user.Email = model.Email;
        user.UserName = model.Email;

        IdentityResult updateResult = await m_userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (IdentityError error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = await buildRoleOptionsAsync(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList());
            ret = View(model);
            return ret;
        }

        bool currentLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
        IdentityResult lockResult = await applyLockoutAsync(user, model.IsLocked);
        if (!lockResult.Succeeded)
        {
            foreach (IdentityError error in lockResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = await buildRoleOptionsAsync(model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList());
            ret = View(model);
            return ret;
        }

        IList<string> currentRoles = await m_userManager.GetRolesAsync(user);
        List<string> selectedRoles = model.Roles
            .Where(r => r.IsSelected)
            .Select(r => r.RoleName)
            .ToList();

        IEnumerable<string> toAdd = selectedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> toRemove = currentRoles.Except(selectedRoles, StringComparer.OrdinalIgnoreCase);
        bool rolesChanged = toAdd.Any() || toRemove.Any();

        IdentityResult rolesResult = IdentityResult.Success;
        if (toAdd.Any())
        {
            rolesResult = await m_userManager.AddToRolesAsync(user, toAdd);
        }

        if (rolesResult.Succeeded && toRemove.Any())
        {
            rolesResult = await m_userManager.RemoveFromRolesAsync(user, toRemove);
        }

        if (!rolesResult.Succeeded)
        {
            foreach (IdentityError error in rolesResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Roles = await buildRoleOptionsAsync(selectedRoles);
            ret = View(model);
            return ret;
        }

        bool lockStatusChanged = currentLocked != model.IsLocked;
        if (lockStatusChanged || rolesChanged)
        {
            await m_userManager.UpdateSecurityStampAsync(user);
        }

        await refreshCurrentUserSessionAsync(user, model.IsLocked);

        TempData["Success"] = m_localizer["Notification_UserUpdated", user.Email ?? string.Empty].Value;
        ret = RedirectToAction(nameof(Index));
        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        IActionResult ret;
        ApplicationUser? user = await m_userManager.FindByIdAsync(id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        UserResetPasswordViewModel model = new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty
        };

        ret = View(model);
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
    {
        IActionResult ret;
        if (!ModelState.IsValid)
        {
            ret = View(model);
            return ret;
        }

        ApplicationUser? user = await m_userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        string token = await m_userManager.GeneratePasswordResetTokenAsync(user);
        IdentityResult resetResult = await m_userManager.ResetPasswordAsync(user, token, model.Password);
        if (!resetResult.Succeeded)
        {
            foreach (IdentityError error in resetResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ret = View(model);
            return ret;
        }

        TempData["Success"] = m_localizer["Notification_PasswordReset", user.Email ?? string.Empty].Value;
        ret = RedirectToAction(nameof(Index));
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string id, bool lockUser)
    {
        IActionResult ret;
        ApplicationUser? user = await m_userManager.FindByIdAsync(id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["Error_UserNotFound"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        bool currentLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value.UtcDateTime > DateTime.UtcNow;
        IdentityResult lockResult = await applyLockoutAsync(user, lockUser);
        if (!lockResult.Succeeded)
        {
            TempData["Error"] = m_localizer["Error_StatusUpdateFailed"].Value;
            ret = RedirectToAction(nameof(Index));
            return ret;
        }

        if (currentLocked != lockUser)
        {
            await m_userManager.UpdateSecurityStampAsync(user);
        }

        await refreshCurrentUserSessionAsync(user, lockUser);

        TempData["Success"] = m_localizer["Notification_StatusUpdated", user.Email ?? string.Empty].Value;
        ret = RedirectToAction(nameof(Index));
        return ret;
    }

    private async Task<IdentityResult> applyLockoutAsync(ApplicationUser in_user, bool in_isLocked)
    {
        DateTimeOffset? lockoutEnd = in_isLocked ? DateTimeOffset.MaxValue : null;
        IdentityResult result = await m_userManager.SetLockoutEndDateAsync(in_user, lockoutEnd);
        if (!result.Succeeded)
        {
            m_logger.LogWarning("Не удалось обновить статус блокировки пользователя {UserId}: {Errors}", in_user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result;
    }

    private async Task<List<UserRoleOptionViewModel>> buildRoleOptionsAsync(IList<string>? in_selectedRoles)
    {
        List<UserRoleOptionViewModel> ret = new();
        List<IdentityRole> roles = await m_roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        HashSet<string> selected = new(StringComparer.OrdinalIgnoreCase);
        if (in_selectedRoles is not null)
        {
            foreach (string role in in_selectedRoles)
            {
                selected.Add(role);
            }
        }

        foreach (IdentityRole role in roles)
        {
            UserRoleOptionViewModel option = new()
            {
                RoleName = role.Name ?? string.Empty,
                DisplayName = role.Name ?? string.Empty,
                IsSelected = selected.Contains(role.Name ?? string.Empty)
            };
            ret.Add(option);
        }

        return ret;
    }

    private async Task refreshCurrentUserSessionAsync(ApplicationUser in_user, bool in_isLocked)
    {
        string? currentUserId = m_userManager.GetUserId(User);
        bool isCurrentUser = string.Equals(in_user.Id, currentUserId, StringComparison.Ordinal);
        if (isCurrentUser)
        {
            if (in_isLocked)
            {
                await m_signInManager.SignOutAsync();
            }
            else
            {
                AuthenticateResult authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                AuthenticationProperties properties;
                if (authResult.Succeeded && authResult.Properties is not null)
                {
                    Dictionary<string, string?> items = new(authResult.Properties.Items, StringComparer.Ordinal);
                    properties = new AuthenticationProperties(items)
                    {
                        AllowRefresh = authResult.Properties.AllowRefresh,
                        IsPersistent = authResult.Properties.IsPersistent,
                        ExpiresUtc = authResult.Properties.ExpiresUtc,
                        IssuedUtc = DateTimeOffset.UtcNow
                    };
                }
                else
                {
                    properties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        IssuedUtc = DateTimeOffset.UtcNow
                    };
                }

                ClaimsPrincipal principal = await m_signInManager.CreateUserPrincipalAsync(in_user);
                await m_signInManager.SignOutAsync();
                await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, properties);
            }
        }
    }
}
