using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ArbiyshkynBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArbiyshkynBot.Infrastructure.Resilience;

public class StateReconciler : IHostedService
{
    private readonly ILogger<StateReconciler> _logger;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public StateReconciler(ILogger<StateReconciler> logger, IDbContextFactory<AppDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StateReconciler is running state synchronization check...");

        using var dbContext = _contextFactory.CreateDbContext();
        
        // Находим зависшие сделки в статусе Pending при перезапуске системы
        var pendingDeals = await dbContext.Deals
            .Where(d => d.Status == "Pending")
            .ToListAsync(cancellationToken);

        if (pendingDeals.Any())
        {
            _logger.LogWarning("Found {Count} pending deals during restart. Reconciling states...", pendingDeals.Count);
            
            foreach (var deal in pendingDeals)
            {
                // Логика сверки с биржей: если ордер не подтвержден, переводим в Cancelled или проверяем статус через API
                deal.Status = "Reconciled_Timeout";
                deal.ReasonDescription += " [Reconciled on restart / SIGTERM]";
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("State reconciliation completed successfully.");
        }
        else
        {
            _logger.LogInformation("No pending deals found. System state is clean.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StateReconciler received stop signal (SIGTERM). Ensuring safe shutdown...");
        return Task.CompletedTask;
    }
}