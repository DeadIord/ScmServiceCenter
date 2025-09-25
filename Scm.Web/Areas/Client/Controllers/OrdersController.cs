using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Web.Areas.Client.Models;

namespace Scm.Web.Areas.Client.Controllers;

[Area("Client")]
[AllowAnonymous]
public class OrdersController : Controller
{
    private readonly ScmDbContext _dbContext;
    private readonly IQuoteService _quoteService;
    private readonly IMessageService _messageService;

    public OrdersController(ScmDbContext dbContext, IQuoteService quoteService, IMessageService messageService)
    {
        _dbContext = dbContext;
        _quoteService = quoteService;
        _messageService = messageService;
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? number, string? token)
    {
        ClientOrderViewModel model = new();

        if (!string.IsNullOrWhiteSpace(number) && !string.IsNullOrWhiteSpace(token))
        {
            var order = await _dbContext.Orders
                .Include(o => o.QuoteLines)
                .Include(o => o.Messages.OrderByDescending(m => m.At))
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Number == number && o.ClientAccessToken == token);

            if (order is null)
            {
                ViewBag.Error = "Заказ не найден. Проверьте номер и токен доступа.";
            }
            else
            {
                model = new ClientOrderViewModel
                {
                    Order = order,
                    QuoteLines = order.QuoteLines.OrderBy(l => l.Title).ToList(),
                    Messages = order.Messages.OrderByDescending(m => m.At).ToList()
                };
            }
        }

        ViewBag.Number = number;
        ViewBag.Token = token;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveLine(Guid lineId, string number, string token)
    {
        var line = await _dbContext.QuoteLines.Include(l => l.Order).FirstOrDefaultAsync(l => l.Id == lineId);
        if (line?.Order is null || line.Order.Number != number || line.Order.ClientAccessToken != token)
        {
            TempData["Error"] = "Строка не найдена";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        if (line.Status != QuoteLineStatus.Proposed)
        {
            TempData["Error"] = "Строка уже обработана";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        await _quoteService.ApproveLineAsync(lineId);
        TempData["Success"] = "Строка согласована";
        return RedirectToAction(nameof(Track), new { number, token });
    }

    [HttpPost]
    public async Task<IActionResult> RejectLine(Guid lineId, string number, string token)
    {
        var line = await _dbContext.QuoteLines.Include(l => l.Order).FirstOrDefaultAsync(l => l.Id == lineId);
        if (line?.Order is null || line.Order.Number != number || line.Order.ClientAccessToken != token)
        {
            TempData["Error"] = "Строка не найдена";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        if (line.Status != QuoteLineStatus.Proposed)
        {
            TempData["Error"] = "Строка уже обработана";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        await _quoteService.RejectLineAsync(lineId);
        TempData["Success"] = "Строка отклонена";
        return RedirectToAction(nameof(Track), new { number, token });
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(MessageDto dto, string number, string token)
    {
        if (dto.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Text))
        {
            TempData["Error"] = "Введите сообщение";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order is null || order.Number != number || order.ClientAccessToken != token)
        {
            TempData["Error"] = "Заказ не найден";
            return RedirectToAction(nameof(Track), new { number, token });
        }

        dto.FromClient = true;
        await _messageService.AddAsync(dto, null);
        TempData["Success"] = "Сообщение отправлено";
        return RedirectToAction(nameof(Track), new { number, token });
    }
}
