using Scm.Domain.Entities;

namespace Scm.Application.DTOs;

public sealed class CreateOrderDto
{
    public string ClientName { get; set; } = string.Empty;

    public string ClientPhone { get; set; } = string.Empty;

    public string Device { get; set; } = string.Empty;

    public string? Serial { get; set; }

    public string Defect { get; set; } = string.Empty;

    public OrderPriority Priority { get; set; } = OrderPriority.Normal;
}
