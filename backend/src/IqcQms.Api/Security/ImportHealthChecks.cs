using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IqcQms.Api.Security;

public sealed class ImportReadinessHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public ImportReadinessHealthCheck(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database connection failed.");
            }

            if (_context.Database.IsRelational())
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                if (pendingMigrations.Any())
                {
                    return HealthCheckResult.Degraded($"Pending database migrations: {string.Join(", ", pendingMigrations)}");
                }
            }

            return HealthCheckResult.Healthy("Import platform database and store are ready.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Readiness check exception", ex);
        }
    }
}

public sealed class ImportOperationalHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public ImportOperationalHealthCheck(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var poisonCount = await _context.PersistentImportWorkItems
                .AsNoTracking()
                .CountAsync(w => w.State == "Poison", cancellationToken);

            var backlogCount = await _context.PersistentImportWorkItems
                .AsNoTracking()
                .CountAsync(w => w.State == "Pending", cancellationToken);

            if (poisonCount > 5)
            {
                return HealthCheckResult.Degraded($"High poison work item count detected: {poisonCount}");
            }

            if (backlogCount > 100)
            {
                return HealthCheckResult.Degraded($"High work item queue backlog detected: {backlogCount}");
            }

            return HealthCheckResult.Healthy($"Work queue operational. Backlog: {backlogCount}, Poison: {poisonCount}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Operational health check exception", ex);
        }
    }
}
