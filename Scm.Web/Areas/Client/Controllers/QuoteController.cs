using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;

namespace Scm.Web.Areas.Client.Controllers;

[Area("Client")]
[AllowAnonymous]
[Route("Client/[controller]")]
public class QuoteController : Controller
{
    private readonly ScmDbContext _dbContext;
    private readonly IQuoteService _quoteService;

    public QuoteController(ScmDbContext dbContext, IQuoteService quoteService)
    {
        _dbContext = dbContext;
        _quoteService = quoteService;
    }

    [HttpPost("Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid in_orderId, string in_token, Guid[]? in_approvedLineIds)
    {
        IActionResult ret;
        var order = await _dbContext.Orders
            .Include(o => o.QuoteLines)
            .FirstOrDefaultAsync(o => o.Id == in_orderId);

        if (order is null || order.ClientAccessToken != in_token)
        {
            TempData["Error"] = "Заказ не найден";
            ret = RedirectToOrder(order?.Number, in_token);
        }
        else
        {
            var hasProposed = order.QuoteLines.Any(l => l.Status == QuoteLineStatus.Proposed);
            if (!hasProposed)
            {
                TempData["Error"] = "Нет строк для подтверждения";
                ret = RedirectToOrder(order.Number, in_token);
            }
            else
            {
                var approvedLineIds = in_approvedLineIds ?? Array.Empty<Guid>();
                await _quoteService.ProcessClientApprovalAsync(order.Id, approvedLineIds);

                TempData["Success"] = "Смета обработана";
                ret = RedirectToOrder(order.Number, in_token);
            }
        }

        return ret;
    }

    private RedirectToActionResult RedirectToOrder(string? in_number, string in_token)
    {
        RedirectToActionResult ret;
        if (string.IsNullOrWhiteSpace(in_number))
        {
            TempData["Error"] ??= "Не удалось найти заказ";
            ret = RedirectToAction("Track", "Orders", new { area = "Client" });
        }
        else
        {
            ret = RedirectToAction("Track", "Orders", new { area = "Client", number = in_number, token = in_token });
        }

        return ret;
    }
}
