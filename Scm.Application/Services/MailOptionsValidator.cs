using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Scm.Application.Services;

public sealed class MailOptionsValidator : IValidateOptions<MailOptions>
{
    private readonly ILogger<MailOptionsValidator> m_logger;

    public MailOptionsValidator(ILogger<MailOptionsValidator> in_logger)
    {
        m_logger = in_logger;
    }

    public ValidateOptionsResult Validate(string in_name, MailOptions in_options)
    {
        var validationMessages = new List<string>();

        if (in_options == null)
        {
            const string message = "Секция Mail не настроена";
            m_logger.LogError(message);
            return ValidateOptionsResult.Fail(message);
        }

        if (string.IsNullOrWhiteSpace(in_options.From))
        {
            validationMessages.Add("Не указан адрес отправителя Mail:From");
        }

        if (string.IsNullOrWhiteSpace(in_options.Host))
        {
            validationMessages.Add("Не указан SMTP-хост Mail:Host");
        }

        if (in_options.Port <= 0)
        {
            validationMessages.Add("Порт SMTP должен быть больше нуля");
        }

        if (string.IsNullOrWhiteSpace(in_options.User))
        {
            validationMessages.Add("Не указан логин SMTP Mail:User");
        }

        var sanitizedPassword = in_options.GetSanitizedPassword();

        if (string.IsNullOrWhiteSpace(sanitizedPassword))
        {
            validationMessages.Add("Не задан пароль приложения Mail:Password");
        }
        else
        {
            if (sanitizedPassword.Length != 16)
            {
                validationMessages.Add("Пароль приложения Gmail должен содержать 16 символов");
            }

            if (sanitizedPassword.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                validationMessages.Add("Пароль приложения должен состоять только из латинских букв и цифр");
            }
        }

        foreach (var validationMessage in validationMessages)
        {
            m_logger.LogError("Ошибка конфигурации SMTP: {Message}", validationMessage);
        }

        ValidateOptionsResult ret;

        if (validationMessages.Count == 0)
        {
            ret = ValidateOptionsResult.Success;
        }
        else
        {
            ret = ValidateOptionsResult.Fail(validationMessages);
        }

        return ret;
    }
}
