using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
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

        var roles = new[] { "Admin", "Manager", "Technician", "Storekeeper", "Support", "Accountant", "Client" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await EnsureUserAsync(userManager, "admin@scm.local", "Администратор", "P@ssw0rd!", new[] { "Admin" });

        if (!await context.Accounts.AnyAsync())
        {
            var demoAccounts = new List<Account>
            {
                new()
                {
                    Name = "ООО \"ТехноСервис\"",
                    Type = AccountType.Company,
                    Inn = "7701234567",
                    Address = "г. Москва, ул. Техническая, 15",
                    Tags = "VIP"
                },
                new()
                {
                    Name = "ИП Иванов Сергей",
                    Type = AccountType.Person,
                    Address = "г. Санкт-Петербург, Невский пр., 100",
                    Tags = "Retail"
                }
            };

            context.Accounts.AddRange(demoAccounts);
            await context.SaveChangesAsync();

            var demoContacts = new List<Contact>
            {
                new()
                {
                    AccountId = demoAccounts[0].Id,
                    FullName = "Анна Петрова",
                    Position = "Офис-менеджер",
                    Phone = "+7 495 111-22-33",
                    Email = "anna.petrowa@technoservice.local"
                },
                new()
                {
                    AccountId = demoAccounts[0].Id,
                    FullName = "Дмитрий Соколов",
                    Position = "Технический директор",
                    Phone = "+7 495 111-44-55",
                    Email = "d.sokolov@technoservice.local"
                },
                new()
                {
                    AccountId = demoAccounts[1].Id,
                    FullName = "Сергей Иванов",
                    Phone = "+7 921 555-66-77",
                    Email = "sergey@ivanov.spb"
                }
            };

            context.Contacts.AddRange(demoContacts);
            await context.SaveChangesAsync();
        }

        if (!await context.Orders.AnyAsync())
        {
            var priorities = new[] { OrderPriority.Low, OrderPriority.Normal, OrderPriority.High, OrderPriority.Critical };
            var statuses = Enum.GetValues<OrderStatus>();
            var rnd = new Random(42);

            var orders = new List<Order>();
            var accounts = await context.Accounts.AsNoTracking().ToListAsync();
            var contacts = await context.Contacts.AsNoTracking().ToListAsync();
            for (int i = 1; i <= 10; i++)
            {
                var status = statuses[rnd.Next(statuses.Length)];
                Account? account = accounts.Count > 0 ? accounts[rnd.Next(accounts.Count)] : null;
                Contact? contact = account is not null
                    ? contacts.FirstOrDefault(c => c.AccountId == account.Id)
                    : null;
                var order = new Order
                {
                    Number = $"SRV-{DateTime.UtcNow:yyyy}-{i:D4}",
                    ClientName = $"Клиент {i}",
                    ClientPhone = $"+7 900 000-0{i:D2}",
                    AccountId = account?.Id,
                    ContactId = contact?.Id,
                    Device = i % 2 == 0 ? "Смартфон" : "Ноутбук",
                    Serial = i % 3 == 0 ? $"SN{i:000000}" : null,
                    Defect = "Устройство не включается",
                    Priority = priorities[rnd.Next(priorities.Length)],
                    Status = status,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-rnd.Next(1, 20)),
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
                    AtUtc = order.CreatedAtUtc.AddHours(1)
                });

                context.Messages.Add(new Message
                {
                    OrderId = order.Id,
                    FromClient = true,
                    Text = "Спасибо за оперативность!",
                    AtUtc = order.CreatedAtUtc.AddHours(5)
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

        if (!await context.ReportDefinitions.AnyAsync())
        {
            var reportSamples = new List<ReportDefinition>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Открытые заказы",
                    Description = "Перечень активных заказов с приоритетом и сроком SLA",
                    SqlText = "SELECT o.\"Number\", o.\"ClientName\", o.\"Status\", o.\"Priority\", o.\"CreatedAtUtc\", o.\"SLAUntil\" FROM \"Orders\" o WHERE (@status IS NULL OR o.\"Status\" = @status) ORDER BY o.\"CreatedAtUtc\" DESC",
                    ParametersJson = JsonSerializer.Serialize(new[]
                    {
                        new ReportParameterDefinition { Name = "@status", Type = "int", DefaultValue = null }
                    }),
                    Visibility = ReportVisibility.Team,
                    AllowedRolesJson = JsonSerializer.Serialize(new[] { "Manager", "Admin" }),
                    CreatedBy = admin.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                    IsActive = true
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Выручка по заказам",
                    Description = "Сумма утверждённых работ и запчастей по каждому заказу",
                    SqlText = "SELECT o.\"Number\" AS \"Номер заказа\", SUM(q.\"Price\" * q.\"Qty\") AS \"Выручка\" FROM \"Orders\" o JOIN \"QuoteLines\" q ON q.\"OrderId\" = o.\"Id\" WHERE q.\"Status\" = 2 AND (@from IS NULL OR o.\"CreatedAtUtc\" >= @from) AND (@to IS NULL OR o.\"CreatedAtUtc\" < @to + INTERVAL '1 day') GROUP BY o.\"Number\" ORDER BY \"Выручка\" DESC",
                    ParametersJson = JsonSerializer.Serialize(new[]
                    {
                        new ReportParameterDefinition { Name = "@from", Type = "date" },
                        new ReportParameterDefinition { Name = "@to", Type = "date" }
                    }),
                    Visibility = ReportVisibility.Organization,
                    AllowedRolesJson = JsonSerializer.Serialize(Array.Empty<string>()),
                    CreatedBy = admin.Id,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                }
            };

            context.ReportDefinitions.AddRange(reportSamples);
            await context.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        IEnumerable<string>? userRoles = null)
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

        if (userRoles is not null)
        {
            foreach (var role in userRoles)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }

        return user;
    }
}
