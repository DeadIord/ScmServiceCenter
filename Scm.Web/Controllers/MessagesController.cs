using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Web.Authorization;
using Scm.Web.Security;

namespace Scm.Web.Controllers;

[Authorize(Policy = PolicyNames.MessagesAccess)]
[ApiController]
[Route("[controller]")]
public sealed class MessagesController(
    IMessageService messageService,
    IOrderService orderService,
    IClientOrderAccessService clientOrderAccessService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;
    private readonly IOrderService _orderService = orderService;
    private readonly IClientOrderAccessService _clientOrderAccessService = clientOrderAccessService;

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromBody] AddMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Текст сообщения обязателен" });
        }

        var order = await _orderService.GetAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return BadRequest(new { message = "Заказ не найден" });
        }

        var clientFilter = await _clientOrderAccessService.GetFilterAsync(User, cancellationToken);
        if (User.IsInRole("Client") && !_clientOrderAccessService.CanAccessOrder(order, clientFilter, request.Token))
        {
            return Forbid();
        }

        var dto = new MessageDto
        {
            OrderId = request.OrderId,
            Text = request.Text,
            FromClient = User.IsInRole("Client")
        };

        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var message = await _messageService.AddAsync(dto, userId, cancellationToken);
            return Ok(new
            {
                message.Id,
                message.OrderId,
                message.FromUserId,
                message.FromClient,
                message.Text,
                message.AtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class AddMessageRequest
    {
        public Guid OrderId { get; set; }

        public string Text { get; set; } = string.Empty;

        public string? Token { get; set; }
    }
}
