using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Scm.Infrastructure.Identity;
using Scm.Web.Areas.Admin.Models.Users;
using Scm.Web.Authorization;
using Scm.Web.Localization;

namespace Scm.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = RolePolicies.RequireAdministrator)]
public sealed class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> m_userManager;
    private readonly RoleManager<IdentityRole> m_roleManager;
    private readonly IStringLocalizer<SharedResource> m_localizer;

    public UsersController(
        UserManager<ApplicationUser> in_userManager,
        RoleManager<IdentityRole> in_roleManager,
        IStringLocalizer<SharedResource> in_localizer)
    {
        m_userManager = in_userManager;
        m_roleManager = in_roleManager;
        m_localizer = in_localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IActionResult ret;
        var users = await m_userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();
        var items = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await m_userManager.GetRolesAsync(user);
            var isActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
            var item = new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                PhoneNumber = user.PhoneNumber,
                IsActive = isActive,
                Roles = roles.ToList()
            };
            items.Add(item);
        }

        var roleNames = await m_roleManager.Roles
            .Select(role => role.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToListAsync();

        var model = new UsersIndexViewModel
        {
            Users = items,
            RoleNames = roleNames,
            RoleRulesDescription = m_localizer["RoleRulesDescription"]
        };
        ret = View(model);
        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        IActionResult ret;
        var model = new UserCreateViewModel
        {
            Roles = await buildRoleSelectionsAsync(null)
        };
        ret = View(model);
        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel in_model)
    {
        IActionResult ret;
        var selectedRoleNames = (in_model.Roles ?? new List<RoleSelectionViewModel>())
            .Where(role => role.Selected)
            .Select(role => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        if (!ModelState.IsValid)
        {
            in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
            ret = View(in_model);
        }
        else
        {
            var user = new ApplicationUser
            {
                DisplayName = in_model.DisplayName,
                Email = in_model.Email,
                UserName = in_model.Email,
                PhoneNumber = in_model.PhoneNumber,
                EmailConfirmed = true
            };
            var createResult = await m_userManager.CreateAsync(user, in_model.Password);
            if (!createResult.Succeeded)
            {
                appendErrors(createResult.Errors);
                in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
                ret = View(in_model);
            }
            else
            {
                var rolesAssigned = true;
                if (selectedRoleNames.Count > 0)
                {
                    var addResult = await m_userManager.AddToRolesAsync(user, selectedRoleNames);
                    if (!addResult.Succeeded)
                    {
                        appendErrors(addResult.Errors);
                        rolesAssigned = false;
                    }
                }

                if (!rolesAssigned || !ModelState.IsValid)
                {
                    in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
                    ret = View(in_model);
                }
                else
                {
                    TempData["Success"] = m_localizer["UserCreated"];
                    ret = RedirectToAction(nameof(Index));
                }
            }
        }

        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        IActionResult ret;
        var user = await m_userManager.FindByIdAsync(id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["UserNotFound"];
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            var currentRoles = await m_userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Roles = await buildRoleSelectionsAsync(currentRoles)
            };
            ret = View(model);
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel in_model)
    {
        IActionResult ret;
        var selectedRoleNames = (in_model.Roles ?? new List<RoleSelectionViewModel>())
            .Where(role => role.Selected)
            .Select(role => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        if (!ModelState.IsValid)
        {
            in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
            ret = View(in_model);
        }
        else
        {
            var user = await m_userManager.FindByIdAsync(id);
            if (user is null)
            {
                TempData["Error"] = m_localizer["UserNotFound"];
                ret = RedirectToAction(nameof(Index));
            }
            else
            {
                user.DisplayName = in_model.DisplayName;
                user.Email = in_model.Email;
                user.UserName = in_model.Email;
                user.PhoneNumber = in_model.PhoneNumber;

                var updateResult = await m_userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    appendErrors(updateResult.Errors);
                    in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
                    ret = View(in_model);
                }
                else
                {
                    var rolesUpdated = await updateUserRolesAsync(user, selectedRoleNames);
                    if (!rolesUpdated)
                    {
                        in_model.Roles = await buildRoleSelectionsAsync(selectedRoleNames);
                        ret = View(in_model);
                    }
                    else
                    {
                        TempData["Success"] = m_localizer["UserUpdated"];
                        ret = RedirectToAction(nameof(Index));
                    }
                }
            }
        }

        return ret;
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        IActionResult ret;
        var user = await m_userManager.FindByIdAsync(id);
        if (user is null)
        {
            TempData["Error"] = m_localizer["UserNotFound"];
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            var model = new ResetPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty
            };
            ret = View(model);
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel in_model)
    {
        IActionResult ret;
        if (!ModelState.IsValid)
        {
            ret = View(in_model);
        }
        else
        {
            var user = await m_userManager.FindByIdAsync(in_model.UserId);
            if (user is null)
            {
                TempData["Error"] = m_localizer["UserNotFound"];
                ret = RedirectToAction(nameof(Index));
            }
            else
            {
                var token = await m_userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await m_userManager.ResetPasswordAsync(user, token, in_model.Password);
                if (!resetResult.Succeeded)
                {
                    appendErrors(resetResult.Errors);
                    ret = View(in_model);
                }
                else
                {
                    TempData["Success"] = m_localizer["PasswordResetSuccess"];
                    ret = RedirectToAction(nameof(Edit), new { id = in_model.UserId });
                }
            }
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoles(RoleAssignmentInputModel in_model)
    {
        IActionResult ret;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = m_localizer["RolesUpdateError"];
            ret = RedirectToAction(nameof(Index));
        }
        else
        {
            var user = await m_userManager.FindByIdAsync(in_model.UserId);
            if (user is null)
            {
                TempData["Error"] = m_localizer["UserNotFound"];
                ret = RedirectToAction(nameof(Index));
            }
            else
            {
                var rolesUpdated = await updateUserRolesAsync(user, in_model.SelectedRoles);
                if (rolesUpdated)
                {
                    TempData["Success"] = m_localizer["RolesUpdated"];
                }
                else
                {
                    var errors = ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToList();
                    if (errors.Any())
                    {
                        TempData["Error"] = string.Join("\n", errors);
                    }
                    else if (!TempData.ContainsKey("Error"))
                    {
                        TempData["Error"] = m_localizer["RolesUpdateError"];
                    }
                }

                ret = RedirectToAction(nameof(Index));
            }
        }

        return ret;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UserStatusInputModel in_model)
    {
        IActionResult ret;
        var isSuccess = false;
        if (!ModelState.IsValid)
        {
            TempData["Error"] = m_localizer["StatusUpdateError"];
        }
        else
        {
            var user = await m_userManager.FindByIdAsync(in_model.UserId);
            if (user is null)
            {
                TempData["Error"] = m_localizer["UserNotFound"];
            }
            else
            {
                if (in_model.IsActive)
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = null;
                }
                else
                {
                    user.LockoutEnabled = true;
                    user.LockoutEnd = DateTimeOffset.MaxValue;
                }

                var updateResult = await m_userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    TempData["Success"] = m_localizer["StatusUpdated"];
                    isSuccess = true;
                }
                else
                {
                    appendErrors(updateResult.Errors);
                    var errors = ModelState.Values
                        .SelectMany(value => value.Errors)
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .ToList();
                    if (errors.Any())
                    {
                        TempData["Error"] = string.Join("\n", errors);
                    }
                    else
                    {
                        TempData["Error"] = m_localizer["StatusUpdateError"];
                    }
                }
            }
        }

        if (!isSuccess && !TempData.ContainsKey("Error"))
        {
            TempData["Error"] = m_localizer["StatusUpdateError"];
        }

        ret = RedirectToAction(nameof(Index));
        return ret;
    }

    private async Task<List<RoleSelectionViewModel>> buildRoleSelectionsAsync(IEnumerable<string>? in_selectedRoles)
    {
        var ret = new List<RoleSelectionViewModel>();
        var selected = new HashSet<string>(in_selectedRoles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var roles = await m_roleManager.Roles
            .Select(role => role.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToListAsync();
        foreach (var role in roles)
        {
            var item = new RoleSelectionViewModel
            {
                Name = role,
                Selected = selected.Contains(role)
            };
            ret.Add(item);
        }

        return ret;
    }

    private async Task<bool> updateUserRolesAsync(ApplicationUser in_user, IEnumerable<string> in_selectedRoles)
    {
        var ret = true;
        var selected = new HashSet<string>(in_selectedRoles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var currentRoles = await m_userManager.GetRolesAsync(in_user);
        var toAdd = selected.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = currentRoles.Except(selected, StringComparer.OrdinalIgnoreCase).ToList();

        if (toAdd.Count > 0)
        {
            var addResult = await m_userManager.AddToRolesAsync(in_user, toAdd);
            if (!addResult.Succeeded)
            {
                appendErrors(addResult.Errors);
                ret = false;
            }
        }

        if (ret && toRemove.Count > 0)
        {
            var removeResult = await m_userManager.RemoveFromRolesAsync(in_user, toRemove);
            if (!removeResult.Succeeded)
            {
                appendErrors(removeResult.Errors);
                ret = false;
            }
        }

        return ret;
    }

    private void appendErrors(IEnumerable<IdentityError> in_errors)
    {
        foreach (var error in in_errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
