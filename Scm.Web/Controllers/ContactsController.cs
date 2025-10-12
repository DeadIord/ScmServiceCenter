using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Web.Authorization;
using Scm.Web.Models.Crm;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.CrmAccess)]
public sealed class ContactsController : Controller
{
    private readonly IContactService _contactService;
    private readonly IAccountService _accountService;

    public ContactsController(IContactService contactService, IAccountService accountService)
    {
        _contactService = contactService;
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? accountId, string? q)
    {
        var contacts = await _contactService.GetListAsync(accountId, q);
        var accounts = await _accountService.GetListAsync(null);

        var model = new ContactIndexViewModel
        {
            AccountId = accountId,
            Query = q,
            Contacts = contacts,
            Accounts = accounts
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var contact = await _contactService.GetAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        var model = new ContactDetailsViewModel
        {
            Contact = contact
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? accountId)
    {
        var dto = new ContactInputDto
        {
            AccountId = accountId ?? Guid.Empty
        };

        await PopulateAccountsAsync(dto.AccountId);
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContactInputDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountsAsync(dto.AccountId);
            return View(dto);
        }

        var contact = await _contactService.CreateAsync(dto);
        TempData["Success"] = "Контакт создан";
        return RedirectToAction(nameof(Details), new { id = contact.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var contact = await _contactService.GetAsync(id);
        if (contact is null)
        {
            return NotFound();
        }

        var dto = new ContactInputDto
        {
            AccountId = contact.AccountId,
            FullName = contact.FullName,
            Position = contact.Position,
            Phone = contact.Phone,
            Email = contact.Email
        };

        await PopulateAccountsAsync(dto.AccountId);
        ViewBag.ContactId = id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ContactInputDto dto)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountsAsync(dto.AccountId);
            ViewBag.ContactId = id;
            return View(dto);
        }

        await _contactService.UpdateAsync(id, dto);
        TempData["Success"] = "Контакт обновлён";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateAccountsAsync(Guid selected)
    {
        var accounts = await _accountService.GetListAsync(null);
        ViewBag.Accounts = accounts
            .Select(a => new SelectListItem
            {
                Text = a.Name,
                Value = a.Id.ToString(),
                Selected = a.Id == selected
            })
            .Prepend(new SelectListItem
            {
                Text = "— Выберите контрагента —",
                Value = string.Empty,
                Selected = selected == Guid.Empty
            })
            .ToList();
    }
}
