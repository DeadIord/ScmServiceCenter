using System.Text.RegularExpressions;
using Scm.Application.DTOs;

namespace Scm.Application.Validators;

public sealed class CreateOrderDtoValidator : IValidator<CreateOrderDto>
{
    public IEnumerable<string> Validate(CreateOrderDto instance)
    {
        if (string.IsNullOrWhiteSpace(instance.ClientName))
        {
            yield return "Имя клиента обязательно";
        }
        else if (instance.ClientName.Length > 128)
        {
            yield return "Имя клиента слишком длинное";
        }

        if (string.IsNullOrWhiteSpace(instance.ClientPhone))
        {
            yield return "Телефон обязателен";
        }
        else if (instance.ClientPhone.Length != 11)
        {
            yield return "Телефон должен содержать 11 цифр";
        }
        else if (!Regex.IsMatch(instance.ClientPhone, @"^[0-9]{11}$"))
        {
            yield return "Телефон должен состоять только из цифр";
        }

        if (string.IsNullOrWhiteSpace(instance.ClientEmail))
        {
            yield return "Email обязателен";
        }
        else if (instance.ClientEmail.Length > 128)
        {
            yield return "Email слишком длинный";
        }
        else if (!Regex.IsMatch(instance.ClientEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
        {
            yield return "Email указан в неверном формате";
        }

        if (string.IsNullOrWhiteSpace(instance.Device))
        {
            yield return "Устройство обязательно";
        }
        else if (instance.Device.Length > 128)
        {
            yield return "Название устройства слишком длинное";
        }

        if (instance.Serial is { Length: > 64 })
        {
            yield return "Серийный номер слишком длинный";
        }

        if (string.IsNullOrWhiteSpace(instance.Defect))
        {
            yield return "Описание неисправности обязательно";
        }
        else if (instance.Defect.Length > 500)
        {
            yield return "Описание неисправности слишком длинное";
        }
    }
}
