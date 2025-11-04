using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Web.Controllers;
using Scm.Web.Models.Tickets;
using Scm.Web.Services;
using Scm.Domain.Entities;
using Xunit;

namespace Scm.Web.Tests;

public sealed class TicketsControllerTests
{
    [Fact]
    public async Task Reply_WithValidFormData_CallsServiceWithTicketId()
    {
        var ticketServiceMock = new Mock<ITicketService>();
        ticketServiceMock
            .Setup(service => service.AddAgentReplyAsync(It.IsAny<Guid>(), It.IsAny<TicketReplyDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketMessage());

        var loggerMock = new Mock<ILogger<TicketsController>>();
        var pollerMock = new Mock<ITicketInboxPoller>();
        var controller = new TicketsController(ticketServiceMock.Object, pollerMock.Object, loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "agent-1"),
            new Claim(ClaimTypes.Name, "Agent One")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var ticketId = Guid.NewGuid();
        var replyBody = "Ответ для клиента";
        var attachmentContent = new MemoryStream(Encoding.UTF8.GetBytes("file-content"));
        var attachment = new FormFile(attachmentContent, 0, attachmentContent.Length, "data", "note.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var inputModel = new TicketReplyInputModel
        {
            TicketId = ticketId,
            Subject = "Test subject",
            Body = replyBody,
            ReplyToExternalId = "external-123",
            Attachments = new List<IFormFile> { attachment }
        };

        var result = await controller.Reply(inputModel, CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(TicketsController.Index), redirectResult.ActionName);
        Assert.Equal(ticketId, redirectResult.RouteValues?["id"]);

        ticketServiceMock.Verify(
            service => service.AddAgentReplyAsync(
                It.Is<Guid>(in_ticketId => in_ticketId == ticketId),
                It.Is<TicketReplyDto>(in_reply => in_reply.BodyHtml == replyBody && in_reply.Attachments.Count == 1),
                It.Is<string>(in_userId => in_userId == "agent-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task Compose_WithValidFormData_CreatesTicket()
    {
        var ticketServiceMock = new Mock<ITicketService>();
        ticketServiceMock
            .Setup(service => service.CreateTicketAsync(It.IsAny<TicketComposeDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketMessage());

        var loggerMock = new Mock<ILogger<TicketsController>>();
        var pollerMock = new Mock<ITicketInboxPoller>();
        var controller = new TicketsController(ticketServiceMock.Object, pollerMock.Object, loggerMock.Object);

        var httpContext = new DefaultHttpContext();
        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "agent-1"),
            new Claim(ClaimTypes.Name, "Agent One")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var attachmentContent = new MemoryStream(Encoding.UTF8.GetBytes("file-content"));
        var attachment = new FormFile(attachmentContent, 0, attachmentContent.Length, "data", "note.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var composeModel = new TicketComposeInputModel
        {
            Email = "client@example.com",
            ClientName = "Client",
            Subject = "Order 123",
            Body = "Message body",
            Attachments = new List<IFormFile> { attachment }
        };

        var result = await controller.Compose(composeModel, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var successProperty = okResult.Value?.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        var successValue = successProperty?.GetValue(okResult.Value) as bool?;
        Assert.True(successValue);

        ticketServiceMock.Verify(
            service => service.CreateTicketAsync(
                It.Is<TicketComposeDto>(dto => dto.ClientEmail == composeModel.Email && dto.Subject == composeModel.Subject),
                "agent-1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
