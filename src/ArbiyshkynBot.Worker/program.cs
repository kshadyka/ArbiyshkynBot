using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ArbiyshkynBot.Infrastructure.Persistence;
using ArbiyshkynBot.Infrastructure.Exchanges.Xt;
using ArbiyshkynBot.Infrastructure.Exchanges.BrowserAutomation;
using ArbiyshkynBot.Infrastructure.Ai;
using ArbiyshkynBot.Infrastructure.Resilience;
using ArbiyshkynBot.Worker;
using ArbiyshkynBot.Worker.WebUI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация базы данных (SQLite)
var connectionString = builder.Configuration["DB_CONNECTION_STRING"] ?? "Data Source=app.db";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Регистрация сервисов и клиентов инфраструктуры
builder.Services.AddHttpClient();

builder.Services.AddSingleton<XtRestClient>(sp => 
    new XtRestClient(
        sp.GetRequiredService<HttpClient>(),
        builder.Configuration["XT_API_KEY"] ?? string.Empty,
        builder.Configuration["XT_API_SECRET"] ?? string.Empty,
        sp.GetRequiredService<ILogger<XtRestClient>>()
    ));

builder.Services.AddSingleton<PlaywrightService>();
builder.Services.AddSingleton<IAiAnalyzer, OpenRouterAiAnalyzer>(sp => 
    new OpenRouterAiAnalyzer(
        sp.GetRequiredService<HttpClient>(), 
        builder.Configuration["OPENROUTER_API_KEY"] ?? string.Empty,
        sp.GetRequiredService<ILogger<OpenRouterAiAnalyzer>>()
    ));

// 3. Регистрация сервисов устойчивости и фоновых воркеров
builder.Services.AddHostedService<PanicStopService>();
builder.Services.AddHostedService<MarketScanner>();
builder.Services.AddHostedService<StateReconciler>();

// 4. Health Checks для Docker
builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Автоматическое применение EF Core миграций при старте
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// 5. Подключение middleware безопасности (Белый список IP + Защита)
app.UseMiddleware<IpWhitelistMiddleware>();

// Эндпоинт проверки здоровья для Docker (возвращает 200 OK)
app.MapHealthChecks("/health");

app.MapGet("/", () => "ArbiyshkynBot Core Engine is running (.NET 10).");

app.Run();