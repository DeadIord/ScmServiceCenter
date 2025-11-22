using Microsoft.Extensions.Options;
using Scm.Domain.Entities;

namespace Scm.Application.Services;

public sealed class MoneyConverter : IMoneyConverter
{
    private readonly CurrencyOptions _options;

    public MoneyConverter(IOptions<CurrencyOptions>? in_options = null)
    {
        _options = in_options?.Value ?? new CurrencyOptions();
        if (!_options.Rates.ContainsKey(_options.BaseCurrency))
        {
            _options.Rates[_options.BaseCurrency] = 1m;
        }
    }

    public string BaseCurrency => _options.BaseCurrency.ToUpperInvariant();

    public Money Convert(Money in_money, string in_targetCurrency)
    {
        var targetCurrency = NormalizeCurrency(in_targetCurrency);
        var sourceCurrency = NormalizeCurrency(in_money.Currency);

        var sourceRate = ResolveRate(sourceCurrency);
        var targetRate = ResolveRate(targetCurrency);

        var baseAmount = in_money.Amount * sourceRate;
        var targetAmount = targetRate == 0
            ? 0m
            : baseAmount / targetRate;

        var ret = new Money(targetAmount, targetCurrency);
        return ret;
    }

    public IReadOnlyCollection<string> GetCurrencies()
    {
        var ret = _options.Rates.Keys
            .Select(NormalizeCurrency)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToArray();

        return ret;
    }

    public string NormalizeCurrency(string? in_currency)
    {
        var ret = string.IsNullOrWhiteSpace(in_currency)
            ? BaseCurrency
            : in_currency.ToUpperInvariant();

        return ret;
    }

    public Money Sum(IEnumerable<Money> in_amounts, string in_targetCurrency)
    {
        var targetCurrency = NormalizeCurrency(in_targetCurrency);
        var total = 0m;

        foreach (var item in in_amounts)
        {
            var converted = Convert(item, targetCurrency);
            total += converted.Amount;
        }

        var ret = new Money(total, targetCurrency);
        return ret;
    }

    private decimal ResolveRate(string in_currency)
    {
        var currency = NormalizeCurrency(in_currency);

        if (!_options.Rates.TryGetValue(currency, out var rate))
        {
            rate = _options.Rates.GetValueOrDefault(BaseCurrency, 1m);
        }

        return rate;
    }
}
