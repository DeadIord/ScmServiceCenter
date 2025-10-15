using Scm.Application.DTOs;

namespace Scm.Application.Validators;

public sealed class AddQuoteLineDtoValidator : IValidator<AddQuoteLineDto>
{
    public IEnumerable<string> Validate(AddQuoteLineDto instance)
    {
        if (instance.OrderId == Guid.Empty)
        {
            yield return "Не указан заказ";
        }

        if (string.IsNullOrWhiteSpace(instance.Title))
        {
            yield return "Название обязательно";
        }
        else if (instance.Title.Length > 200)
        {
            yield return "Название слишком длинное";
        }

        if (instance.Qty <= 0)
        {
            yield return "Количество должно быть больше нуля";
        }

        if (instance.Price < 0)
        {
            yield return "Цена не может быть отрицательной";
        }
    }
}
