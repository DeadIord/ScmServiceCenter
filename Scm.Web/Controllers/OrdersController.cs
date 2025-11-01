using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Web.Models.Orders;
using Scm.Web.Authorization;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.OrdersAccess)]
public class OrdersController : Controller
{
    private readonly IOrderService m_orderService;
    private readonly IQuoteService m_quoteService;
    private readonly IMessageService m_messageService;
    private readonly IAccountService m_accountService;
    private readonly IContactService m_contactService;
    private readonly IMailService m_mailService;
    private readonly ILogger<OrdersController> m_logger;
    private readonly IStringLocalizer<OrdersController> m_localizer;
    public OrdersController(
        IOrderService in_orderService,
        IQuoteService in_quoteService,
        IMessageService in_messageService,
        IAccountService in_accountService,
        IContactService in_contactService,
        IMailService in_mailService,
        ILogger<OrdersController> in_logger,
        IStringLocalizer<OrdersController> in_localizer)
    {
        m_orderService = in_orderService;
        m_quoteService = in_quoteService;
        m_messageService = in_messageService;
        m_accountService = in_accountService;
        m_contactService = in_contactService;
        m_mailService = in_mailService;
        m_logger = in_logger;
        m_localizer = in_localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, OrderStatus? status, int page = 1)
    {
        const int pageSize = 20;
        var pagedOrders = await m_orderService.GetQueuePageAsync(q, status, page, pageSize);
        var model = new OrdersIndexViewModel
        {
            Query = q,
            Status = status,
            Orders = pagedOrders.Items.Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                Number = o.Number,
                ClientName = o.ClientName,
                ClientPhone = o.ClientPhone,
                ClientEmail = o.ClientEmail,
                Device = o.Device,
                Status = o.Status,
                Priority = o.Priority,
                SLAUntil = o.SLAUntil,
                CreatedAtUtc = o.CreatedAtUtc
            }).ToList(),
            PageNumber = pagedOrders.PageNumber,
            TotalPages = pagedOrders.TotalPages,
            PageSize = pagedOrders.PageSize,
            TotalCount = pagedOrders.TotalCount,
            StartRecord = pagedOrders.StartRecord,
            EndRecord = pagedOrders.EndRecord
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? accountId, Guid? contactId, string? accountSearch, string? contactSearch)
    {
        var dto = new CreateOrderDto();

        if (contactId.HasValue && contactId.Value != Guid.Empty)
        {
            var contact = await m_contactService.GetAsync(contactId.Value);
            if (contact is not null)
            {
                dto.ContactId = contact.Id;
                dto.AccountId = contact.AccountId;
                dto.ClientName = contact.FullName;
                dto.ClientPhone = contact.Phone;
                dto.ClientEmail = contact.Email;
                accountId = contact.AccountId;
            }
        }
        else if (accountId.HasValue && accountId.Value != Guid.Empty)
        {
            dto.AccountId = accountId;
        }

        await PopulateCrmSelectionsAsync(dto.AccountId, accountSearch, dto.ContactId, contactSearch);
        ViewData["AccountSearch"] = accountSearch;
        ViewData["ContactSearch"] = contactSearch;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto, string? accountSearch, string? contactSearch)
    {
        if (dto.ContactId.HasValue && dto.ContactId.Value != Guid.Empty)
        {
            var contact = await m_contactService.GetAsync(dto.ContactId.Value);
            if (contact is null)
            {
                ModelState.AddModelError(nameof(dto.ContactId), m_localizer["Error_ContactNotFound"].Value);
            }
            else
            {
                if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty && dto.AccountId.Value != contact.AccountId)
                {
                    ModelState.AddModelError(nameof(dto.ContactId), m_localizer["Error_ContactDoesNotBelong"].Value);
                }
                else
                {
                    dto.AccountId = contact.AccountId;
                    if (string.IsNullOrWhiteSpace(dto.ClientEmail))
                    {
                        dto.ClientEmail = contact.Email;
                    }
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateCrmSelectionsAsync(dto.AccountId, accountSearch, dto.ContactId, contactSearch);
            ViewData["AccountSearch"] = accountSearch;
            ViewData["ContactSearch"] = contactSearch;
            return View(dto);
        }

        var order = await m_orderService.CreateAsync(dto);
        TempData["Success"] = m_localizer["Notification_OrderCreated", order.Number].Value;
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var order = await m_orderService.GetAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var messages = await m_messageService.GetForOrderAsync(id);
        var total = await m_quoteService.GetTotalAsync(id);

        var trackingLink = BuildTrackingLink(order);

        var model = new OrderDetailsViewModel
        {
            Order = order,
            Messages = messages,
            ApprovedTotal = total,
            ClientTrackingLink = trackingLink
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrdersAccess)]
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatus to)
    {
        try
        {
            await m_orderService.ChangeStatusAsync(id, to);
            return Json(new { ok = true, status = to.ToString() });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { ok = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddQuoteLine(AddQuoteLineDto dto)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
            return Json(new { success = false, errors });
        }

        try
        {
            await m_quoteService.AddLineAsync(dto);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitQuote(Guid orderId)
    {
        try
        {
            await m_quoteService.SubmitForApprovalAsync(orderId);
            var order = await m_orderService.GetAsync(orderId);
            if (order is null)
            {
                throw new InvalidOperationException(m_localizer["Error_OrderNotFound"].Value);
            }

            var recipientEmail = string.IsNullOrWhiteSpace(order.ClientEmail)
                ? order.Contact?.Email
                : order.ClientEmail;

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new InvalidOperationException(m_localizer["Error_ClientEmailMissing"].Value);
            }

            var trackingLink = BuildTrackingLink(order);
            if (string.IsNullOrWhiteSpace(trackingLink))
            {
                throw new InvalidOperationException(m_localizer["Error_ClientLinkFailed"].Value);
            }
            var subject = m_localizer["Email_Subject", order.Number].Value;
            var body = BuildClientApprovalEmail(order.ClientName, order.Device, trackingLink);

            await m_mailService.SendAsync(
                recipientEmail,
                subject,
                body,
                false,
                HttpContext.RequestAborted);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to send approval request for order {OrderId}", orderId);
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> GenerateInvoice(Guid id)
    {
        try
        {
            var invoice = await m_orderService.CreateInvoiceAsync(id);
            var url = Url.Action(nameof(Invoice), new { orderId = id, invoiceId = invoice.Id });
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Не удалось сформировать ссылку на счёт");
            }

            return Json(new { success = true, url });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Invoice(Guid orderId, Guid invoiceId)
    {
        var order = await m_orderService.GetAsync(orderId);
        if (order is null)
        {
            return NotFound();
        }

        var invoice = order.Invoices.FirstOrDefault(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return NotFound();
        }

        var lines = order.QuoteLines
            .Where(l => l.Status == QuoteLineStatus.Approved)
            .OrderBy(l => l.Kind)
            .ThenBy(l => l.Title)
            .ToList();

        if (!lines.Any())
        {
            TempData["Error"] = "Нет утверждённых строк сметы для формирования счёта.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        var model = new OrderInvoiceViewModel
        {
            Order = order,
            Invoice = invoice,
            Lines = lines,
            Total = lines.Sum(l => l.Price * l.Qty)
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Technician")]
    public async Task<IActionResult> Kanban()
    {
        var orders = await m_orderService.GetQueueAsync(null, null);
        var grouped = orders
            .GroupBy(o => o.Status)
            .Select(g => new OrderKanbanColumnViewModel
            {
                Status = g.Key,
                Title = GetStatusTitle(g.Key),
                Orders = g.ToList()
            })
            .ToList();

        var columns = Enum.GetValues<OrderStatus>()
            .Select(status => grouped.FirstOrDefault(g => g.Status == status) ?? new OrderKanbanColumnViewModel
            {
                Status = status,
                Title = GetStatusTitle(status),
                Orders = Array.Empty<Order>()
            })
            .ToList();

        var model = new OrderKanbanViewModel { Columns = columns };
        return View(model);
    }

    private async Task PopulateCrmSelectionsAsync(Guid? accountId, string? accountSearch, Guid? contactId, string? contactSearch)
    {
        var accounts = await m_accountService.SearchAsync(accountSearch, 50);
        if (accountId.HasValue && accountId.Value != Guid.Empty && accounts.All(a => a.Id != accountId.Value))
        {
            var selectedAccount = await m_accountService.GetAsync(accountId.Value);
            if (selectedAccount is not null)
            {
                accounts.Insert(0, selectedAccount);
            }
        }

        ViewBag.Accounts = accounts
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem
            {
                Text = a.Name,
                Value = a.Id.ToString(),
                Selected = accountId.HasValue && accountId.Value == a.Id
            })
            .Prepend(new SelectListItem
            {
                Text = m_localizer["Selection_NoAccount"].Value,
                Value = string.Empty,
                Selected = !accountId.HasValue || accountId.Value == Guid.Empty
            })
            .ToList();

        var contacts = accountId.HasValue && accountId.Value != Guid.Empty
            ? await m_contactService.GetForAccountAsync(accountId.Value, contactSearch, 50)
            : (await m_contactService.GetListAsync(null, contactSearch)).Take(50).ToList();

        if (contactId.HasValue && contactId.Value != Guid.Empty && contacts.All(c => c.Id != contactId.Value))
        {
            var selectedContact = await m_contactService.GetAsync(contactId.Value);
            if (selectedContact is not null)
            {
                contacts.Insert(0, selectedContact);
            }
        }

        ViewBag.Contacts = contacts
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem
            {
                Text = c.FullName,
                Value = c.Id.ToString(),
                Selected = contactId.HasValue && contactId.Value == c.Id
            })
            .Prepend(new SelectListItem
            {
                Text = m_localizer["Selection_NoContact"].Value,
                Value = string.Empty,
                Selected = !contactId.HasValue || contactId.Value == Guid.Empty
            })
            .ToList();
    }

    private string GetStatusTitle(OrderStatus in_status)
    {
        string ret;
        var key = $"Status_{in_status}";
        var localized = m_localizer[key];
        if (localized.ResourceNotFound)
        {
            ret = in_status.ToString();
        }
        else
        {
            ret = localized.Value;
        }

        return ret;
    }

    private string BuildTrackingLink(Order in_order)
    {
        var ret = Url.Action(
            action: "Track",
            controller: "Orders",
            values: new { area = "Client", number = in_order.Number, token = in_order.ClientAccessToken },
            protocol: Request.Scheme) ?? string.Empty;
        return ret;
    }

    private string BuildClientApprovalEmail(string in_clientName, string in_device, string in_trackingLink)
    {
        string ret;
        var newline = Environment.NewLine + Environment.NewLine;
        var parts = new[]
        {
            m_localizer["Email_Greeting", in_clientName].Value,
            m_localizer["Email_Intro", in_device].Value,
            in_trackingLink,
            m_localizer["Email_Questions"].Value,
            m_localizer["Email_Signature"].Value
        };
        ret = string.Join(newline, parts);
        return ret;
    }
}
