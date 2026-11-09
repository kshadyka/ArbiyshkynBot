using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ArbiyshkynBot.Infrastructure.Exchanges.Xt;
using ArbiyshkynBot.Infrastructure.Ai;

namespace ArbiyshkynBot.Worker;

public class MarketScanner : BackgroundService
{
    private readonly ILogger<MarketScanner> _logger;
    private readonly XtRestClient _xtRestClient;
    private readonly IAiAnalyzer _aiAnalyzer;
    private readonly TimeSpan _scanInterval = TimeSpan.FromSeconds(15);

    public MarketScanner(
        ILogger<MarketScanner> logger,
        XtRestClient xtRestClient,
        IAiAnalyzer aiAnalyzer)
    {
        _logger = logger;
        _xtRestClient = xtRestClient;
        _aiAnalyzer = aiAnalyzer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketScanner background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Пример сканирования волатильной пары из конфигурации (например, ECR_USDT или NEAR_USDT)
                string symbol = "ecr_usdt";
                
                var symbolData = await _xtRestClient.GetSymbolRulesAsync(symbol);
                if (!string.IsNullOrEmpty(symbolData))
                {
                    _logger.LogDebug("Scanned symbol {Symbol} successfully.", symbol);
                    
                    // Запрос оценки у AI перед потенциальным входом
                    decimal aiConfidence = await _aiAnalyzer.GetConfidenceScoreAsync(symbol, "RSI < 28, Volume > 500k, Bollinger bounce detected.");
                    
                    if (aiConfidence > 0.75m)
                    {
                        _logger.LogInformation("AI High Confidence signal received ({Score}) for {Symbol}. Triggering evaluation...", aiConfidence, symbol);
                        // Здесь срабатывает триггер на открытие ордера в TradingEngine
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during market scanning cycle.");
            }

            await Task.Delay(_scanInterval, stoppingToken);
        }
    }
}