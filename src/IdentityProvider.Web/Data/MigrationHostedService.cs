using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Web.Data;

public sealed class MigrationHostedService : IHostedService
{
    // Stay under IIS ANCM startupTimeLimit (120s) while Azure SQL serverless resumes from auto-pause.
    private static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(90);

    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(IServiceProvider services, ILogger<MigrationHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(3);
        var deadline = DateTime.UtcNow + MaxWait;

        while (true)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _logger.LogInformation("Applying database migrations.");
                await db.Database.MigrateAsync(cancellationToken);
                await SeedData.EnsureSeededAsync(scope.ServiceProvider, cancellationToken);
                _logger.LogInformation("Database is ready.");
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && DateTime.UtcNow + delay < deadline && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Database is not ready yet (Azure SQL auto-pause resume). Retrying in {DelaySeconds}s.", delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 15));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool IsTransient(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.IsTransient)
            {
                return true;
            }

            if (current is InvalidOperationException &&
                current.Message.Contains("transient", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
