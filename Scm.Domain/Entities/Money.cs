namespace Scm.Domain.Entities;

public readonly record struct Money
{
    public Money(decimal in_amount, string in_currency)
    {
        Amount = Math.Round(in_amount, 2, MidpointRounding.AwayFromZero);
        Currency = string.IsNullOrWhiteSpace(in_currency)
            ? "RUB"
            : in_currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public override string ToString()
    {
        var ret = $"{Amount:N2} {Currency}";
        return ret;
    }
}
