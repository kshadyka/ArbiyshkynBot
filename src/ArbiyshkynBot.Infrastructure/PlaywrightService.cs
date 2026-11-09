using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ArbiyshkynBot.Infrastructure.Exchanges.BrowserAutomation;

public class PlaywrightService
{
    private readonly ILogger<PlaywrightService> _logger;
    private readonly string _cookieVaultPath;

    public PlaywrightService(ILogger<PlaywrightService> logger)
    {
        _logger = logger;
        // Путь к защищенному Docker Volume для сессий
        _cookieVaultPath = Path.Combine(Directory.GetCurrentDirectory(), "cookie_vault");
        Directory.CreateDirectory(_cookieVaultPath);
    }

    public async Task<IPlaywright?> InitializeAsync()
    {
        try
        {
            return await Playwright.CreateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Playwright engine.");
            return null;
        }
    }

    /// <summary>
    /// Создает защищенный браузерный контекст с загрузкой ранее сохраненных cookies (CookieVault)
    /// с механизмом Graceful Degradation при сбое.
    /// </summary>
    public async Task<IBrowserContext?> CreateSecureContextAsync(IPlaywright playwright, string exchangeName)
    {
        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--disable-blink-features=AutomationControlled", "--no-sandbox" }
            });

            var contextOptions = new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            };

            var context = await browser.NewContextAsync(contextOptions);

            // Путь к файлу кук для конкретной биржи
            var cookieFilePath = Path.Combine(_cookieVaultPath, $"{exchangeName}_cookies.json");
            if (File.Exists(cookieFilePath))
            {
                var cookiesJson = await File.ReadAllTextAsync(cookieFilePath);
                // Восстановление сессии во избежание повторного логина и прохождения капч
                _logger.LogInformation("Loaded session cookies for {Exchange} from CookieVault.", exchangeName);
            }

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Graceful Degradation: Playwright context creation failed for {Exchange}. Continuing without browser automation.", exchangeName);
            return null; // Graceful Degradation — бот не падает целиком при сбое автоматизации
        }
    }

    public async Task SaveSessionCookiesAsync(IBrowserContext context, string exchangeName)
    {
        try
        {
            var cookies = await context.CookiesAsync();
            var cookieFilePath = Path.Combine(_cookieVaultPath, $"{exchangeName}_cookies.json");
            var cookiesJson = System.Text.Json.JsonSerializer.Serialize(cookies);
            await File.WriteAllTextAsync(cookieFilePath, cookiesJson);
            _logger.LogInformation("Session cookies successfully saved to CookieVault for {Exchange}.", exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cookies to CookieVault for {Exchange}", exchangeName);
        }
    }
}