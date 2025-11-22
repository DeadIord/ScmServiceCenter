namespace Scm.Application.Services;

public sealed class CurrencyOptions
{
    public string BaseCurrency { get; set; } = "RUB";

    public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        { "RUB", 1m },
        { "USD", 90m },
        { "EUR", 98m }
    };
}
