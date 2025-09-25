using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Application.Validators;
using Scm.Domain.Entities;
using Scm.Web.Models.Orders;

namespace Scm.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly IQuoteService _quoteService;
    private readonly IMessageService _messageService;
    private readonly CreateOrderDtoValidator _createValidator;
    private readonly AddQuoteLineDtoValidator _quoteValidator;

    public OrdersController(
        IOrderService orderService,
        IQuoteService quoteService,
        IMessageService messageService,
        CreateOrderDtoValidator createValidator,
        AddQuoteLineDtoValidator quoteValidator)
    {
        _orderService = orderService;
        _quoteService = quoteService;
        _messageService = messageService;
        _createValidator = createValidator;
        _quoteValidator = quoteValidator;
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
                CreatedAt = o.CreatedAt
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
        var errors = _createValidator.Validate(dto).ToList();
        if (errors.Any())
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

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
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatus to)
    {
        try
        {
            await _orderService.ChangeStatusAsync(id, to);
            return Json(new { success = true, status = to.ToString() });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddQuoteLine(AddQuoteLineDto dto)
    {
        var errors = _quoteValidator.Validate(dto).ToList();
        if (errors.Any())
        {
            Response.StatusCode = 400;
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
