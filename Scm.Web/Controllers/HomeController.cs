using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Infrastructure.Persistence;
using Scm.Web.Models;
using Scm.Web.Models.Home;

namespace Scm.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOrderService _orderService;
        private readonly ScmDbContext _dbContext;

        public HomeController(
            ILogger<HomeController> logger,
            IOrderService orderService,
            ScmDbContext dbContext) 
        {
            _logger = logger;
            _orderService = orderService;
            _dbContext = dbContext; 
        }

        public async Task<IActionResult> Index()
        {
            // Получаем все заказы
            var allOrders = await _orderService.GetQueueAsync(null, null);

            // Получаем общее количество запчастей на складе
            // ВАЖНО: В базе поле называется StockQty, а не Quantity!
            var partsCount = await _dbContext.Parts.SumAsync(p => (int)p.StockQty);

            // Считаем метрики
            var now = DateTime.UtcNow;
            var newOrders = allOrders.Count(o => o.Status == OrderStatus.Received);
            var inProgress = allOrders.Count(o =>
                o.Status == OrderStatus.Diagnosing ||
                o.Status == OrderStatus.InRepair ||
                o.Status == OrderStatus.WaitingApproval);
            var overdue = allOrders.Count(o =>
                o.SLAUntil.HasValue &&
                o.SLAUntil.Value < now &&
                o.Status != OrderStatus.Closed &&
                o.Status != OrderStatus.Ready);

            // Берем последние 10 активных заказов
            var recentOrders = allOrders
                .Where(o => o.Status != OrderStatus.Closed)
                .OrderByDescending(o => o.CreatedAtUtc)
                .Take(10)
                .Select(o => new RecentOrderViewModel
                {
                    Id = o.Id,
                    Number = o.Number,
                    ClientName = o.ClientName,
                    Device = o.Device,
                    Status = o.Status,
                    Priority = o.Priority,
                    CreatedAtUtc = o.CreatedAtUtc,
                    SLAUntil = o.SLAUntil
                })
                .ToList();

            var model = new HomeIndexViewModel
            {
                NewOrdersCount = newOrders,
                InProgressCount = inProgress,
                PartsInStockCount = partsCount,
                OverdueCount = overdue,
                RecentOrders = recentOrders
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}