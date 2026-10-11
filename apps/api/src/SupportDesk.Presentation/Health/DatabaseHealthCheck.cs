using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Presentation.Health;

/// <summary>
/// Reports whether SQL Server accepts a connection. It does not run a business query.
/// A failure is Unhealthy, which the health endpoint returns as HTTP 503.
/// </summary>
public sealed class DatabaseHealthCheck(SupportDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reachable = await db.Database.CanConnectAsync(cancellationToken);

            return reachable
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database is not reachable.", exception);
        }
    }
}
