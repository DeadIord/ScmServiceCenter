using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Domain.Identity;
using Scm.Infrastructure.Persistence;
using Scm.Web.Controllers;
using Scm.Web.Models.Orders;
using Scm.Web.Security;
using Xunit;

namespace Scm.Web.Tests;

public sealed class ClientAccessTests : IDisposable
{
    private readonly ScmDbContext m_dbContext;
    private readonly OrderService m_orderService;
    private readonly ContactService m_contactService;
    private readonly MessageService m_messageService;
    private readonly Mock<UserManager<ApplicationUser>> m_userManager;
    private readonly ClientOrderAccessService m_clientAccessService;

    public ClientAccessTests()
    {
        var options = new DbContextOptionsBuilder<ScmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        m_dbContext = new ScmDbContext(options);
        m_orderService = new OrderService(m_dbContext, Mock.Of<ITechnicianTaskService>());
        m_contactService = new ContactService(m_dbContext);
        m_messageService = new MessageService(m_dbContext);
        m_userManager = CreateUserManager();
        m_clientAccessService = new ClientOrderAccessService(m_contactService, m_userManager.Object);
    }

    [Fact]
    public async Task Index_FiltersOrdersForClient()
    {
        var contact = new Contact
        {
            AccountId = Guid.NewGuid(),
            FullName = "Тестовый клиент",
            Phone = "79000000001",
            Email = "client@example.com"
        };
        var otherContact = new Contact
        {
            AccountId = Guid.NewGuid(),
            FullName = "Другой клиент",
            Phone = "79000000002",
            Email = "other@example.com"
        };
        m_dbContext.Contacts.AddRange(contact, otherContact);

        var ownedOrder = new Order
        {
            Number = "SRV-2024-0001",
            ClientName = "Тестовый клиент",
            ClientPhone = contact.Phone,
            ClientEmail = contact.Email,
            ContactId = contact.Id,
            Device = "Смартфон",
            Defect = "Не включается",
            Status = OrderStatus.Received,
            CreatedAtUtc = DateTime.UtcNow
        };
        var foreignOrder = new Order
        {
            Number = "SRV-2024-0002",
            ClientName = "Другой клиент",
            ClientPhone = otherContact.Phone,
            ClientEmail = otherContact.Email,
            ContactId = otherContact.Id,
            Device = "Ноутбук",
            Defect = "Не заряжается",
            Status = OrderStatus.Received,
            CreatedAtUtc = DateTime.UtcNow
        };

        m_dbContext.Orders.AddRange(ownedOrder, foreignOrder);
        await m_dbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = contact.Email,
            PhoneNumber = contact.Phone
        };
        m_userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var quoteService = Mock.Of<IQuoteService>();
        var accountService = Mock.Of<IAccountService>();
        var mailService = Mock.Of<IMailService>();
        var roleManager = new Mock<RoleManager<IdentityRole>>(Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
        var logger = Mock.Of<ILogger<OrdersController>>();
        var localizer = BuildLocalizer<OrdersController>();

        var controller = new OrdersController(
            m_orderService,
            quoteService,
            m_messageService,
            accountService,
            m_contactService,
            mailService,
            m_userManager.Object,
            roleManager.Object,
            logger,
            localizer.Object,
            m_clientAccessService)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildClientPrincipal(user.Id)
                }
            }
        };

        var result = await controller.Index(null, null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<OrdersIndexViewModel>(viewResult.Model);
        Assert.Single(model.Orders);
        Assert.Equal(ownedOrder.Id, model.Orders.First().Id);
    }

    [Fact]
    public async Task MessagesController_BlocksForeignOrderForClient()
    {
        var contact = new Contact
        {
            AccountId = Guid.NewGuid(),
            FullName = "Тестовый клиент",
            Phone = "79000000003",
            Email = "dialog@example.com"
        };
        var foreignOrder = new Order
        {
            Number = "SRV-2024-0005",
            ClientName = "Сторонний",
            ClientPhone = "79000000099",
            ClientEmail = "otherdialog@example.com",
            Device = "Планшет",
            Defect = "Не включается",
            Status = OrderStatus.Received,
            CreatedAtUtc = DateTime.UtcNow
        };

        m_dbContext.Contacts.Add(contact);
        m_dbContext.Orders.Add(foreignOrder);
        await m_dbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = contact.Email,
            PhoneNumber = contact.Phone
        };
        m_userManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        var controller = new MessagesController(m_messageService, m_orderService, m_clientAccessService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildClientPrincipal(user.Id)
                }
            }
        };

        var response = await controller.Add(new MessagesController.AddMessageRequest
        {
            OrderId = foreignOrder.Id,
            Text = "Нельзя писать в чужую заявку"
        }, CancellationToken.None);

        Assert.IsType<ForbidResult>(response);
    }

    public void Dispose()
    {
        m_dbContext.Dispose();
    }

    private static Mock<IStringLocalizer<T>> BuildLocalizer<T>() where T : class
    {
        var localizer = new Mock<IStringLocalizer<T>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        localizer.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] arguments) => new LocalizedString(key, string.Format(key, arguments)));
        return localizer;
    }

    private static ClaimsPrincipal BuildClientPrincipal(string in_userId)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, in_userId),
            new Claim(ClaimTypes.Role, "Client")
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}
