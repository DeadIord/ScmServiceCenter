using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Microsoft.Extensions.Localization;

namespace Scm.Web.Areas.Client.Controllers;

[Area("Client")]
[AllowAnonymous]
[Route("Client/[controller]")]
public class QuoteController : Controller
{
    private readonly ScmDbContext m_dbContext;
    private readonly IQuoteService m_quoteService;
    private readonly IStringLocalizer<QuoteController> m_localizer;

    public QuoteController(
        ScmDbContext in_dbContext,
        IQuoteService in_quoteService,
        IStringLocalizer<QuoteController> in_localizer)
    {
        m_dbContext = in_dbContext;
        m_quoteService = in_quoteService;
        m_localizer = in_localizer;
    }

    [HttpPost("Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid in_orderId, string in_token, Guid[]? in_approvedLineIds)
    {
        IActionResult ret;
        var order = await m_dbContext.Orders
            .Include(o => o.QuoteLines)
            .FirstOrDefaultAsync(o => o.Id == in_orderId);

        if (order is null || order.ClientAccessToken != in_token)
        {
            TempData["Error"] = m_localizer["Error_OrderNotFound"].Value;
            ret = RedirectToOrder(order?.Number, in_token);
        }
        else
        {
            var hasProposed = order.QuoteLines.Any(l => l.Status == QuoteLineStatus.Proposed);
            if (!hasProposed)
            {
                TempData["Error"] = m_localizer["Error_NoLines"].Value;
                ret = RedirectToOrder(order.Number, in_token);
            }
            else
            {
                var approvedLineIds = in_approvedLineIds ?? Array.Empty<Guid>();
                await m_quoteService.ProcessClientApprovalAsync(order.Id, approvedLineIds);

                TempData["Success"] = m_localizer["Notification_QuoteProcessed"].Value;
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
            TempData["Error"] ??= m_localizer["Error_OrderUnknown"].Value;
            ret = RedirectToAction("Track", "Orders", new { area = "Client" });
        }
        else
        {
            ret = RedirectToAction("Track", "Orders", new { area = "Client", number = in_number, token = in_token });
        }

        return ret;
    }
}
