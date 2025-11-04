using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Application.Validators;
using Scm.Web.Models.Stock;
using Scm.Infrastructure.Persistence;

namespace Scm.Web.Controllers;

[Authorize]
public class StockController : Controller
{
    private readonly ScmDbContext _dbContext;
    private readonly IStockService _stockService;
    private readonly ReceivePartDtoValidator _validator;

    public StockController(ScmDbContext dbContext, IStockService stockService, ReceivePartDtoValidator validator)
    {
        _dbContext = dbContext;
        _stockService = stockService;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var query = _dbContext.Parts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Sku.Contains(term) || p.Title.Contains(term));
        }

        var parts = await query.OrderBy(p => p.Title).ToListAsync();

        var model = new StockIndexViewModel
        {
            Query = q,
            OnlyLowStock = false,
            Parts = parts
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> LowStock()
    {
        var parts = await _stockService.GetLowStockAsync();
        var model = new StockIndexViewModel
        {
            OnlyLowStock = true,
            Parts = parts
        };

        return View("Index", model);
    }

    [HttpGet]
    public IActionResult Receive()
    {
        return PartialView("_ReceiveModal", new ReceivePartDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(ReceivePartDto dto)
    {
        var errors = _validator.Validate(dto).ToList();
        if (errors.Any())
        {
            TempData["Error"] = string.Join("\n", errors);
            return RedirectToAction(nameof(Index), new { q = dto.Sku });
        }

        try
        {
            await _stockService.ReceiveAsync(dto);
            TempData["Success"] = "Приход оформлен";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> ToggleActive([FromBody] ToggleActiveRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Sku))
                return BadRequest(new { success = false });

            var part = await _dbContext.Parts
                .FirstOrDefaultAsync(p => p.Sku == request.Sku);

            if (part == null)
                return NotFound(new { success = false });

            part.IsActive = request.IsActive;
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, isActive = request.IsActive });
        }
        catch
        {
            return StatusCode(500, new { success = false });
        }
    }

    // Класс уже был в вашем коде
    public class ToggleActiveRequest
    {
        public string Sku { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}

