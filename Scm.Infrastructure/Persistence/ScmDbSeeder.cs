using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scm.Domain.Entities;
using Scm.Infrastructure.Identity;

namespace Scm.Infrastructure.Persistence;

public static class ScmDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ScmDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roles = new[] { "Администратор", "Мастер" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await EnsureUserAsync(userManager, "admin@scm.local", "Администратор", "Admin@123", roles[0]);
        var master = await EnsureUserAsync(userManager, "master@scm.local", "Мастер", "Master@123", roles[1]);
        _ = master;
        await EnsureUserAsync(userManager, "client@scm.local", "Клиент", "Client@123");

        if (!await context.Orders.AnyAsync())
        {
            var priorities = new[] { OrderPriority.Low, OrderPriority.Normal, OrderPriority.High, OrderPriority.Critical };
            var statuses = Enum.GetValues<OrderStatus>();
            var rnd = new Random(42);

            var orders = new List<Order>();
            for (int i = 1; i <= 10; i++)
            {
                var status = statuses[rnd.Next(statuses.Length)];
                var order = new Order
                {
                    Number = $"SRV-{DateTime.UtcNow:yyyy}-{i:D4}",
                    ClientName = $"Клиент {i}",
                    ClientPhone = $"+7 900 000-0{i:D2}",
                    Device = i % 2 == 0 ? "Смартфон" : "Ноутбук",
                    Serial = i % 3 == 0 ? $"SN{i:000000}" : null,
                    Defect = "Устройство не включается",
                    Priority = priorities[rnd.Next(priorities.Length)],
                    Status = status,
                    CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 20)),
                    SLAUntil = DateTime.UtcNow.AddDays(rnd.Next(1, 5)),
                };

                orders.Add(order);
            }

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            foreach (var order in orders)
            {
                for (var j = 1; j <= 3; j++)
                {
                    context.QuoteLines.Add(new QuoteLine
                    {
                        OrderId = order.Id,
                        Kind = j % 2 == 0 ? QuoteLineKind.Part : QuoteLineKind.Labor,
                        Title = j % 2 == 0 ? $"Запчасть {j}" : $"Работа {j}",
                        Qty = j,
                        Price = 1500 + j * 500,
                        Status = j == 3 ? QuoteLineStatus.Proposed : QuoteLineStatus.Approved
                    });
                }

                context.Messages.Add(new Message
                {
                    OrderId = order.Id,
                    FromClient = false,
                    FromUserId = admin.Id,
                    Text = "Заказ принят в работу",
                    At = order.CreatedAt.AddHours(1)
                });

                context.Messages.Add(new Message
                {
                    OrderId = order.Id,
                    FromClient = true,
                    Text = "Спасибо за оперативность!",
                    At = order.CreatedAt.AddHours(5)
                });

                context.Invoices.Add(new Invoice
                {
                    OrderId = order.Id,
                    Amount = 5000 + rnd.Next(1000, 5000),
                    Status = InvoiceStatus.Draft,
                    CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 10))
                });
            }

            await context.SaveChangesAsync();
        }

        if (!await context.Parts.AnyAsync())
        {
            var parts = new List<Part>();
            for (int i = 1; i <= 15; i++)
            {
                parts.Add(new Part
                {
                    Sku = $"PART-{i:000}",
                    Title = $"Деталь {i}",
                    StockQty = i % 5 == 0 ? 1 : 10 + i,
                    ReorderPoint = 5,
                    PriceIn = 500 + i * 20,
                    PriceOut = 700 + i * 30,
                    Unit = "шт",
                    IsActive = true
                });
            }

            context.Parts.AddRange(parts);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string displayName, string password, string? role = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName
            };

            await userManager.CreateAsync(user, password);
        }

        if (!string.IsNullOrEmpty(role) && !await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
