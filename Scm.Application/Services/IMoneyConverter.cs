using Scm.Domain.Entities;

namespace Scm.Application.Services;

public interface IMoneyConverter
{
    string BaseCurrency { get; }

    string NormalizeCurrency(string? in_currency);

    Money Convert(Money in_money, string in_targetCurrency);

    Money Sum(IEnumerable<Money> in_amounts, string in_targetCurrency);

    IReadOnlyCollection<string> GetCurrencies();
}
