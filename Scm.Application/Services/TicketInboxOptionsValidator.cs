using Microsoft.Extensions.Options;

namespace Scm.Application.Services;

public sealed class TicketInboxOptionsValidator : IValidateOptions<TicketInboxOptions>
{
    public ValidateOptionsResult Validate(string? in_name, TicketInboxOptions in_options)
    {
        if (!in_options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(in_options.Host))
        {
            return ValidateOptionsResult.Fail("Не указан IMAP сервер для импорта тикетов");
        }

        if (in_options.Port <= 0)
        {
            return ValidateOptionsResult.Fail("Некорректный порт IMAP сервера");
        }

        if (string.IsNullOrWhiteSpace(in_options.User))
        {
            return ValidateOptionsResult.Fail("Не указан логин IMAP пользователя");
        }

        if (string.IsNullOrWhiteSpace(in_options.Password))
        {
            return ValidateOptionsResult.Fail("Не указан пароль IMAP пользователя");
        }

        if (in_options.PollIntervalSeconds < 30)
        {
            return ValidateOptionsResult.Fail("Интервал опроса почты не может быть меньше 30 секунд");
        }

        if (in_options.BatchSize <= 0 || in_options.BatchSize > 100)
        {
            return ValidateOptionsResult.Fail("Количество писем за один опрос должно быть в диапазоне 1-100");
        }

        return ValidateOptionsResult.Success;
    }
}
