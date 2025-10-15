using Scm.Application.DTOs;

namespace Scm.Application.Validators;

public sealed class ReceivePartDtoValidator : IValidator<ReceivePartDto>
{
    public IEnumerable<string> Validate(ReceivePartDto instance)
    {
        if (string.IsNullOrWhiteSpace(instance.Sku))
        {
            yield return "SKU обязателен";
        }
        else if (instance.Sku.Length > 64)
        {
            yield return "SKU слишком длинный";
        }

        if (string.IsNullOrWhiteSpace(instance.Title))
        {
            yield return "Наименование обязательно";
        }
        else if (instance.Title.Length > 200)
        {
            yield return "Наименование слишком длинное";
        }

        if (instance.Unit is null or { Length: 0 })
        {
            yield return "Ед. измерения обязательна";
        }
        else if (instance.Unit.Length > 16)
        {
            yield return "Ед. измерения слишком длинная";
        }

        if (instance.Qty <= 0)
        {
            yield return "Количество должно быть больше нуля";
        }

        if (instance.PriceIn < 0)
        {
            yield return "Закупочная цена не может быть отрицательной";
        }

        if (instance.PriceOut < 0)
        {
            yield return "Розничная цена не может быть отрицательной";
        }
    }
}
