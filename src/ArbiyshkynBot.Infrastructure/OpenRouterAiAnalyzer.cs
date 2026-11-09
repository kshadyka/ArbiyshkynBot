using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ArbiyshkynBot.Infrastructure.Ai;

public interface IAiAnalyzer
{
    Task<decimal> GetConfidenceScoreAsync(string symbol, string marketContext);
}

public class OpenRouterAiAnalyzer : IAiAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OpenRouterAiAnalyzer> _logger;

    public OpenRouterAiAnalyzer(HttpClient httpClient, string apiKey, ILogger<OpenRouterAiAnalyzer> logger)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    }

    public async Task<decimal> GetConfidenceScoreAsync(string symbol, string marketContext)
    {
        try
        {
            var requestBody = new
            {
                model = "anthropic/claude-3-haiku", // Быстрая и эффективная модель для трейдинга
                messages = new[]
                {
                    new { role = "system", content = "You are a crypto risk management AI. Analyze the given market context and output ONLY a confidence score between 0.0 and 1.0 (where 1.0 is highest confidence to buy)." },
                    new { role = "user", content = $"Symbol: {symbol}. Context: {marketContext}" }
                },
                temperature = 0.1
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter API returned non-success status: {StatusCode}", response.StatusCode);
                return 0.5m; // Нейтральный fallback при сбое AI
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (decimal.TryParse(content, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var score))
            {
                return Math.Clamp(score, 0.0m, 1.0m);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenRouter API for symbol {Symbol}", symbol);
        }

        return 0.5m; // Fallback по умолчанию
    }
}