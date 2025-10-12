using System;
using System.Collections.Generic;
using Scm.Domain.Entities;

namespace Scm.Web.Models.Orders;

public sealed class OrderInvoiceViewModel
{
    public Order Order { get; init; } = null!;

    public Invoice Invoice { get; init; } = null!;

    public IReadOnlyCollection<QuoteLine> Lines { get; init; } = Array.Empty<QuoteLine>();

    public decimal Total { get; init; }
}
