using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scm.Application.Services;

namespace Scm.Web.Services;

public sealed class TicketInboxHostedService : BackgroundService
{
    private readonly TicketInboxOptions m_options;
    private readonly ITicketInboxPoller m_poller;
    private readonly ILogger<TicketInboxHostedService> m_logger;

    public TicketInboxHostedService(
        IOptions<TicketInboxOptions> in_options,
        ITicketInboxPoller in_poller,
        ILogger<TicketInboxHostedService> in_logger)
    {
        m_options = in_options.Value;
        m_poller = in_poller;
        m_logger = in_logger;
    }

    protected override async Task ExecuteAsync(CancellationToken in_stoppingToken)
    {
        if (!m_options.Enabled)
        {
            m_logger.LogInformation("Импорт почтовых тикетов отключён в настройках.");
            return;
        }

        m_logger.LogInformation(
            "Импорт почтовых тикетов активирован. Почтовый ящик: {Mailbox} на сервере {Host}:{Port}",
            string.IsNullOrWhiteSpace(m_options.Mailbox) ? "INBOX" : m_options.Mailbox,
            m_options.Host,
            m_options.Port);

        var pollingActive = true;
        while (pollingActive && !in_stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await m_poller.PollAsync(in_stoppingToken);
                m_logger.LogInformation(result.StatusMessage);

                if (result.Enabled)
                {
                    m_logger.LogInformation(
                        "Результат импорта: найдено {Total}, обработано {Processed}, импортировано {Imported}, пропущено {Skipped}, ошибок {Failed}",
                        result.TotalMessages,
                        result.ProcessedMessages,
                        result.ImportedMessages,
                        result.SkippedMessages,
                        result.FailedMessages);
                }
            }
            catch (OperationCanceledException)
            {
                pollingActive = false;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Ошибка при опросе почтового ящика тикетов");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(m_options.PollIntervalSeconds), in_stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Игнорируем отмену задержки
            }
        }

        m_logger.LogInformation("Импорт почтовых тикетов остановлен.");
    }
}
