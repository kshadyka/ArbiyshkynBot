using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArbiyshkynBot.Infrastructure.Resilience;

public class PanicStopService : BackgroundService
{
    private readonly ILogger<PanicStopService> _logger;
    private bool _isPanicStopped = false;

    public PanicStopService(ILogger<PanicStopService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Метод экстренной остановки торговли, вызываемый из API контроллера
    /// </summary>
    public Task TriggerPanicStopAsync(string clientIp, string reason)
    {
        _isPanicStopped = true;
        _logger.LogCritical("🚨 PANIC STOP TRIGGERED! Initiator IP: {ClientIp}. Reason: {Reason}", clientIp, reason);
        
        // Здесь можно добавить логику отмены активных ордеров через XtRestClient 
        // и блокировку торговых воркеров.

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PanicStopService background worker is running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_isPanicStopped)
            {
                // Фоновые действия при активном статусе тревоги
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}