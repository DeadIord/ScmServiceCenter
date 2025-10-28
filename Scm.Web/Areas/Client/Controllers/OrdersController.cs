using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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
    private readonly ScmDbContext m_dbContext;
    private readonly IMessageService m_messageService;
    private readonly IStringLocalizer<OrdersController> m_localizer;

    public OrdersController(
        ScmDbContext in_dbContext,
        IMessageService in_messageService,
        IStringLocalizer<OrdersController> in_localizer)
    {
        m_dbContext = in_dbContext;
        m_messageService = in_messageService;
        m_localizer = in_localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? number, string? token)
    {
        ClientOrderViewModel model = new();

        if (!string.IsNullOrWhiteSpace(number) && !string.IsNullOrWhiteSpace(token))
        {
            var order = await m_dbContext.Orders
                .Include(o => o.QuoteLines)
                .Include(o => o.Messages.OrderByDescending(m => m.AtUtc))
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Number == number && o.ClientAccessToken == token);

            if (order is null)
            {
                ViewBag.Error = m_localizer["Error_OrderNotFound"].Value;
            }
            else
            {
                model = new ClientOrderViewModel
                {
                    Order = order,
                    QuoteLines = order.QuoteLines
                        .Where(l => l.Status == QuoteLineStatus.Proposed)
                        .OrderBy(l => l.Title)
                        .ToList(),
                    Messages = order.Messages.OrderByDescending(m => m.AtUtc).ToList()
                };
            }
        }

        ViewBag.Number = number;
        ViewBag.Token = token;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage(MessageDto dto, string number, string token)
    {
        if (dto.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Text))
        {
            TempData["Error"] = m_localizer["Error_MessageRequired"].Value;
            return RedirectToAction(nameof(Track), new { number, token });
        }

        var order = await m_dbContext.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order is null || order.Number != number || order.ClientAccessToken != token)
        {
            TempData["Error"] = m_localizer["Error_OrderLookup"].Value;
            return RedirectToAction(nameof(Track), new { number, token });
        }

        dto.FromClient = true;
        _ = await m_messageService.AddAsync(dto, null);
        TempData["Success"] = m_localizer["Notification_MessageSent"].Value;
        return RedirectToAction(nameof(Track), new { number, token });
    }
}
