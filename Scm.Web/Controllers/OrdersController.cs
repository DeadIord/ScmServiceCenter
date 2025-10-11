using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Web.Models.Orders;

namespace Scm.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Technician")]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IQuoteService _quoteService;
    private readonly IMessageService _messageService;
    private readonly IAccountService _accountService;
    private readonly IContactService _contactService;
    private readonly IMailService _mailService;
    private readonly ILogger<OrdersController> _logger;
    public OrdersController(
        IOrderService orderService,
        IQuoteService quoteService,
        IMessageService messageService,
        IAccountService accountService,
        IContactService contactService,
        IMailService mailService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _quoteService = quoteService;
        _messageService = messageService;
        _accountService = accountService;
        _contactService = contactService;
        _mailService = mailService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, OrderStatus? status)
    {
        var orders = await _orderService.GetQueueAsync(q, status);
        var model = new OrdersIndexViewModel
        {
            Query = q,
            Status = status,
            Orders = orders.Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                Number = o.Number,
                ClientName = o.ClientName,
                ClientPhone = o.ClientPhone,
                Device = o.Device,
                Status = o.Status,
                Priority = o.Priority,
                SLAUntil = o.SLAUntil,
                CreatedAtUtc = o.CreatedAtUtc
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? accountId, Guid? contactId, string? accountSearch, string? contactSearch)
    {
        var dto = new CreateOrderDto();

        if (contactId.HasValue && contactId.Value != Guid.Empty)
        {
            var contact = await _contactService.GetAsync(contactId.Value);
            if (contact is not null)
            {
                dto.ContactId = contact.Id;
                dto.AccountId = contact.AccountId;
                dto.ClientName = contact.FullName;
                dto.ClientPhone = contact.Phone;
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
            var contact = await _contactService.GetAsync(dto.ContactId.Value);
            if (contact is null)
            {
                ModelState.AddModelError(nameof(dto.ContactId), "Контакт не найден");
            }
            else
            {
                if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty && dto.AccountId.Value != contact.AccountId)
                {
                    ModelState.AddModelError(nameof(dto.ContactId), "Контакт не относится к выбранному контрагенту");
                }
                else
                {
                    dto.AccountId = contact.AccountId;
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

        var order = await _orderService.CreateAsync(dto);
        TempData["Success"] = $"Заказ {order.Number} создан";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var order = await _orderService.GetAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var messages = await _messageService.GetForOrderAsync(id);
        var total = await _quoteService.GetTotalAsync(id);

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
    [Authorize(Roles = "Admin,Manager,Technician")]
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatus to)
    {
        try
        {
            await _orderService.ChangeStatusAsync(id, to);
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
            await _quoteService.AddLineAsync(dto);
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
            await _quoteService.SubmitForApprovalAsync(orderId);
            var order = await _orderService.GetAsync(orderId);
            if (order is null)
            {
                throw new InvalidOperationException("Заказ не найден");
            }

            if (order.Contact is null || string.IsNullOrWhiteSpace(order.Contact.Email))
            {
                throw new InvalidOperationException("Для заказа не указан email клиента");
            }

            var trackingLink = BuildTrackingLink(order);
            if (string.IsNullOrWhiteSpace(trackingLink))
            {
                throw new InvalidOperationException("Не удалось сформировать ссылку для клиента");
            }
            var subject = $"Смета для заказа {order.Number}";
            var body = BuildClientApprovalEmail(order.ClientName, order.Device, trackingLink);

            await _mailService.SendAsync(
                order.Contact.Email,
                subject,
                body,
                false,
                HttpContext.RequestAborted);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось отправить смету на согласование для заказа {OrderId}", orderId);
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Technician")]
    public async Task<IActionResult> Kanban()
    {
        var orders = await _orderService.GetQueueAsync(null, null);
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
        var accounts = await _accountService.SearchAsync(accountSearch, 50);
        if (accountId.HasValue && accountId.Value != Guid.Empty && accounts.All(a => a.Id != accountId.Value))
        {
            var selectedAccount = await _accountService.GetAsync(accountId.Value);
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
                Text = "— Без контрагента —",
                Value = string.Empty,
                Selected = !accountId.HasValue || accountId.Value == Guid.Empty
            })
            .ToList();

        var contacts = accountId.HasValue && accountId.Value != Guid.Empty
            ? await _contactService.GetForAccountAsync(accountId.Value, contactSearch, 50)
            : (await _contactService.GetListAsync(null, contactSearch)).Take(50).ToList();

        if (contactId.HasValue && contactId.Value != Guid.Empty && contacts.All(c => c.Id != contactId.Value))
        {
            var selectedContact = await _contactService.GetAsync(contactId.Value);
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
                Text = "— Без контакта —",
                Value = string.Empty,
                Selected = !contactId.HasValue || contactId.Value == Guid.Empty
            })
            .ToList();
    }

    private static string GetStatusTitle(OrderStatus status) => status switch
    {
        OrderStatus.Received => "Получены",
        OrderStatus.Diagnosing => "Диагностика",
        OrderStatus.WaitingApproval => "Ожидают согласования",
        OrderStatus.InRepair => "В ремонте",
        OrderStatus.Ready => "Готовы",
        OrderStatus.Closed => "Закрыты",
        _ => status.ToString()
    };

    private string BuildTrackingLink(Order order)
    {
        var ret = Url.Action(
            action: "Track",
            controller: "Orders",
            values: new { area = "Client", number = order.Number, token = order.ClientAccessToken },
            protocol: Request.Scheme) ?? string.Empty;
        return ret;
    }

    private static string BuildClientApprovalEmail(string clientName, string device, string trackingLink)
    {
        var ret = $"Здравствуйте, {clientName}!" + Environment.NewLine + Environment.NewLine;
        ret += $"Готова смета по ремонту {device}. Перейдите по ссылке, чтобы согласовать работы:" + Environment.NewLine;
        ret += trackingLink + Environment.NewLine + Environment.NewLine;
        ret += "Если у вас есть вопросы, просто ответьте на это письмо." + Environment.NewLine + Environment.NewLine;
        ret += "С уважением,\nСервисный центр";
        return ret;
    }
}
