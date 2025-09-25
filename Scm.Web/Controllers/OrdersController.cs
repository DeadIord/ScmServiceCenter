using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public OrdersController(
        IOrderService orderService,
        IQuoteService quoteService,
        IMessageService messageService)
    {
        _orderService = orderService;
        _quoteService = quoteService;
        _messageService = messageService;
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
    public IActionResult Create()
    {
        return View(new CreateOrderDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
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

        var model = new OrderDetailsViewModel
        {
            Order = order,
            Messages = messages,
            ApprovedTotal = total
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
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(MessageDto dto)
    {
        if (dto.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Text))
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message = "Текст сообщения обязателен" });
        }

        try
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _messageService.AddAsync(dto, userId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
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
}
