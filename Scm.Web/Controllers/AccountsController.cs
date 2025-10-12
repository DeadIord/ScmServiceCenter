using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Web.Authorization;
using Scm.Web.Models.Crm;

namespace Scm.Web.Controllers;

[Authorize(Policy = RolePolicies.OperationsStaff)]
public sealed class AccountsController : Controller
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var accounts = await _accountService.GetListAsync(q);
        var model = new AccountIndexViewModel
        {
            Query = q,
            Accounts = accounts
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var account = await _accountService.GetAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        var model = new AccountDetailsViewModel
        {
            Account = account,
            Contacts = account.Contacts
                .OrderBy(c => c.FullName)
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        PopulateAccountTypes(AccountType.Company);
        return View(new AccountInputDto { Type = AccountType.Company });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountInputDto dto)
    {
        if (!ModelState.IsValid)
        {
            PopulateAccountTypes(dto.Type);
            return View(dto);
        }

        var account = await _accountService.CreateAsync(dto);
        TempData["Success"] = "Контрагент создан";
        return RedirectToAction(nameof(Details), new { id = account.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var account = await _accountService.GetAsync(id);
        if (account is null)
        {
            return NotFound();
        }

        var dto = new AccountInputDto
        {
            Name = account.Name,
            Type = account.Type,
            Inn = account.Inn,
            Address = account.Address,
            Tags = account.Tags,
            ManagerUserId = account.ManagerUserId
        };

        PopulateAccountTypes(dto.Type);
        ViewBag.AccountId = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AccountInputDto dto)
    {
        if (!ModelState.IsValid)
        {
            PopulateAccountTypes(dto.Type);
            ViewBag.AccountId = id;
            return View(dto);
        }

        await _accountService.UpdateAsync(id, dto);
        TempData["Success"] = "Контрагент обновлён";
        return RedirectToAction(nameof(Details), new { id });
    }

    private void PopulateAccountTypes(AccountType selected)
    {
        ViewBag.AccountTypes = Enum.GetValues<AccountType>()
            .Select(t => new SelectListItem
            {
                Text = t switch
                {
                    AccountType.Company => "Компания",
                    AccountType.Person => "Физлицо",
                    _ => t.ToString() ?? string.Empty
                },
                Value = ((int)t).ToString(),
                Selected = t == selected
            })
            .ToList();
    }
}
