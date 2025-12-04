using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Scm.Application.DTOs;
using Scm.Application.Services;
using Scm.Domain.Entities;
using Scm.Domain.Identity;

namespace Scm.Web.Security;

public interface IClientOrderAccessService
{
    Task<ClientOrderAccessFilter?> GetFilterAsync(ClaimsPrincipal in_user, CancellationToken in_cancellationToken = default);

    bool CanAccessOrder(Order in_order, ClientOrderAccessFilter? in_filter, string? in_token);
}

public sealed class ClientOrderAccessService(
    IContactService in_contactService,
    UserManager<ApplicationUser> in_userManager) : IClientOrderAccessService
{
    private readonly IContactService m_contactService = in_contactService;
    private readonly UserManager<ApplicationUser> m_userManager = in_userManager;

    public async Task<ClientOrderAccessFilter?> GetFilterAsync(ClaimsPrincipal in_user, CancellationToken in_cancellationToken = default)
    {
        if (!in_user.IsInRole("Client"))
        {
            return null;
        }

        var userId = in_user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await m_userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var contact = await m_contactService.FindByIdentityAsync(user.Email, user.PhoneNumber, in_cancellationToken);
        return new ClientOrderAccessFilter
        {
            ContactId = contact?.Id,
            Email = NormalizeEmail(user.Email),
            Phone = NormalizePhone(user.PhoneNumber)
        };
    }

    public bool CanAccessOrder(Order in_order, ClientOrderAccessFilter? in_filter, string? in_token)
    {
        if (in_filter is null || !in_filter.HasAny)
        {
            return !string.IsNullOrWhiteSpace(in_token)
                && string.Equals(in_order.ClientAccessToken, in_token, StringComparison.Ordinal);
        }

        var normalizedEmail = NormalizeEmail(in_filter.Email);
        var normalizedPhone = NormalizePhone(in_filter.Phone);

        bool matchesContact = in_filter.ContactId.HasValue
            && in_filter.ContactId.Value != Guid.Empty
            && in_order.ContactId.HasValue
            && in_order.ContactId.Value == in_filter.ContactId.Value;

        bool matchesEmail = !string.IsNullOrWhiteSpace(normalizedEmail)
            && ((!string.IsNullOrWhiteSpace(in_order.ClientEmail)
                    && string.Equals(in_order.ClientEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
                || (in_order.Contact != null
                    && string.Equals(in_order.Contact.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));

        bool matchesPhone = !string.IsNullOrWhiteSpace(normalizedPhone)
            && ((!string.IsNullOrWhiteSpace(in_order.ClientPhone)
                    && string.Equals(in_order.ClientPhone, normalizedPhone, StringComparison.Ordinal))
                || (in_order.Contact != null
                    && string.Equals(in_order.Contact.Phone, normalizedPhone, StringComparison.Ordinal)));

        bool matchesToken = !string.IsNullOrWhiteSpace(in_token)
            && string.Equals(in_order.ClientAccessToken, in_token, StringComparison.Ordinal);

        return matchesContact || matchesEmail || matchesPhone || matchesToken;
    }

    private static string? NormalizeEmail(string? in_email)
    {
        return string.IsNullOrWhiteSpace(in_email) ? null : in_email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? in_phone)
    {
        return string.IsNullOrWhiteSpace(in_phone) ? null : in_phone.Trim();
    }
}
