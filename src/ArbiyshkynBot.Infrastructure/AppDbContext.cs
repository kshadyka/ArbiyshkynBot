using Microsoft.EntityFrameworkCore;

namespace ArbiyshkynBot.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BotSettings> BotSettings => Set<BotSettings>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<SymbolRule> SymbolRules => Set<SymbolRule>();
    public DbSet<TelemetryLog> TelemetryLogs => Set<TelemetryLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Deal>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(d => d.TotalAmount).HasColumnType("decimal(18,4)");
        });

        modelBuilder.Entity<SymbolRule>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Symbol).IsUnique();
        });
    }
}

// Базовые сущности для Core
public class BotSettings
{
    int id;
    public int Id { get => id; set => id = value; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class Deal
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string ReasonDescription { get; set; } = string.Empty;
    public DateTime CreatedAt { get => createdAt; set => createdAt = value; }
    DateTime createdAt = DateTime.UtcNow;
}

public class SymbolRule
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal MinNotional { get; set; }
    public decimal TickSize { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TelemetryLog
{
    public int Id { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}