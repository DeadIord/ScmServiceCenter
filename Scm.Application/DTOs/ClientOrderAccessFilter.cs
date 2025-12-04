using System;

namespace Scm.Application.DTOs;

public sealed class ClientOrderAccessFilter
{
    public Guid? ContactId { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public bool HasAny => (ContactId.HasValue && ContactId.Value != Guid.Empty)
        || !string.IsNullOrWhiteSpace(Email)
        || !string.IsNullOrWhiteSpace(Phone);
}
