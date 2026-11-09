using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ArbiyshkynBot.Infrastructure.Notifications;

public interface INotifier
{
    Task SendAlertAsync(string message, string channel = "all");
}

public class NotificationDispatcher : INotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationDispatcher> _logger;
    private readonly string _telegramToken;
    private readonly string _telegramChatId;
    private readonly string _brotherMesWebhookUrl;

    public NotificationDispatcher(
        HttpClient httpClient,
        ILogger<NotificationDispatcher> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _telegramToken = configuration["TELEGRAM_BOT_TOKEN"] ?? string.Empty;
        _telegramChatId = configuration["TELEGRAM_CHAT_ID"] ?? string.Empty;
        _brotherMesWebhookUrl = configuration["BROTHERMES_WEBHOOK_URL"] ?? "https://brmes.qzde.de/api/webhook";
    }

    public async Task SendAlertAsync(string message, string channel = "all")
    {
        var tasks = new List<Task>();

        if (channel == "all" || channel.Equals("telegram", StringComparison.OrdinalIgnoreCase))
        {
            tasks.Add(SendTelegramAsync(message));
        }

        if (channel == "all" || channel.Equals("brothermes", StringComparison.OrdinalIgnoreCase))
        {
            tasks.Add(SendBrotherMesAsync(message));
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendTelegramAsync(string message)
    {
        if (string.IsNullOrEmpty(_telegramToken) || string.IsNullOrEmpty(_telegramChatId)) return;

        try
        {
            var url = $"https://api.telegram.org/bot{_telegramToken}/sendMessage";
            var payload = new { chat_id = _telegramChatId, text = message, parse_mode = "Markdown" };
            
            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send Telegram alert. StatusCode: {Code}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending Telegram notification.");
        }
    }

    private async Task SendBrotherMesAsync(string message)
    {
        try
        {
            var payload = new { sender = "ArbiyshkynBot", text = message, timestamp = DateTime.UtcNow };
            var response = await _httpClient.PostAsJsonAsync(_brotherMesWebhookUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send BrotherMes webhook alert to brmes.qzde.de.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending BrotherMes notification.");
        }
    }
}