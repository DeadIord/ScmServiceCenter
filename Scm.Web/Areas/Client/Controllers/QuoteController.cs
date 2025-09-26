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

    [HttpPost("Approve")]
    public async Task<IActionResult> Approve(Guid lineId, string token)
    {
        var line = await _dbContext.QuoteLines
            .Include(l => l.Order)
            .FirstOrDefaultAsync(l => l.Id == lineId);

        if (line?.Order is null || line.Order.ClientAccessToken != token)
        {
            TempData["Error"] = "Строка сметы не найдена";
            return RedirectToOrder(line?.Order?.Number, token);
        }

        if (line.Status != QuoteLineStatus.Proposed)
        {
            TempData["Error"] = "Строка уже обработана";
            return RedirectToOrder(line.Order.Number, token);
        }

        await _quoteService.ApproveLineAsync(lineId);
        TempData["Success"] = "Строка согласована";
        return RedirectToOrder(line.Order.Number, token);
    }

    [HttpPost("Reject")]
    public async Task<IActionResult> Reject(Guid lineId, string token)
    {
        var line = await _dbContext.QuoteLines
            .Include(l => l.Order)
            .FirstOrDefaultAsync(l => l.Id == lineId);

        if (line?.Order is null || line.Order.ClientAccessToken != token)
        {
            TempData["Error"] = "Строка сметы не найдена";
            return RedirectToOrder(line?.Order?.Number, token);
        }

        if (line.Status != QuoteLineStatus.Proposed)
        {
            TempData["Error"] = "Строка уже обработана";
            return RedirectToOrder(line.Order.Number, token);
        }

        await _quoteService.RejectLineAsync(lineId);
        TempData["Success"] = "Строка отклонена";
        return RedirectToOrder(line.Order.Number, token);
    }

    private RedirectToActionResult RedirectToOrder(string? number, string token)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            TempData["Error"] ??= "Не удалось найти заказ";
            return RedirectToAction("Track", "Orders", new { area = "Client" });
        }

        return RedirectToAction("Track", "Orders", new { area = "Client", number, token });
    }
}
