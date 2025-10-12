using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scm.Application.Services;

namespace Scm.Web.HealthChecks;

public sealed class MailConfigurationHealthCheck : IHealthCheck
{
    private readonly IMailService m_mailService;
    private readonly IOptions<MailOptions> m_mailOptions;
    private readonly ILogger<MailConfigurationHealthCheck> m_logger;

    public MailConfigurationHealthCheck(
        IMailService in_mailService,
        IOptions<MailOptions> in_mailOptions,
        ILogger<MailConfigurationHealthCheck> in_logger)
    {
        m_mailService = in_mailService;
        m_mailOptions = in_mailOptions;
        m_logger = in_logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext in_context,
        CancellationToken in_cancellationToken = default)
    {
        var options = m_mailOptions.Value;
        HealthCheckResult ret;

        if (!options.EnableHealthCheckProbe)
        {
            const string skippedMessage = "Пробная отправка SMTP отключена";
            ret = HealthCheckResult.Healthy(skippedMessage);
        }
        else if (string.IsNullOrWhiteSpace(options.ProbeRecipient) && string.IsNullOrWhiteSpace(options.From))
        {
            const string recipientMessage = "Не указан адрес для тестовой отправки";
            m_logger.LogError(recipientMessage);
            ret = HealthCheckResult.Unhealthy(recipientMessage);
        }
        else
        {
            var probeRecipient = string.IsNullOrWhiteSpace(options.ProbeRecipient)
                ? options.From
                : options.ProbeRecipient;

            try
            {
                await m_mailService.SendAsync(
                    probeRecipient,
                    "SMTP health-check",
                    "Пробное письмо для проверки SMTP",
                    false,
                    in_cancellationToken);

                ret = HealthCheckResult.Healthy("SMTP отправка выполнена успешно");
            }
            catch (Exception ex)
            {
                const string failureMessage = "Не удалось выполнить тестовую отправку SMTP";
                m_logger.LogError(ex, failureMessage);
                ret = HealthCheckResult.Unhealthy(failureMessage, ex);
            }
        }

        return ret;
    }
}
