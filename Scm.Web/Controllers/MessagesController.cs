using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scm.Application.DTOs;
using Scm.Application.Services;

namespace Scm.Web.Controllers;

[Authorize(Roles = "Admin,Manager,Technician")]
[ApiController]
[Route("[controller]")]
public sealed class MessagesController(IMessageService messageService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;

    [HttpPost("Add")]
    public async Task<IActionResult> Add([FromBody] AddMessageRequest request, CancellationToken cancellationToken)
    {
        if (request.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { message = "Текст сообщения обязателен" });
        }

        var dto = new MessageDto
        {
            OrderId = request.OrderId,
            Text = request.Text,
            FromClient = false
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
    }
}
