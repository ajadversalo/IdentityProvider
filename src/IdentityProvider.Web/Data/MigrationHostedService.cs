using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Web.Data;

public sealed class MigrationHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(IServiceProvider services, ILogger<MigrationHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.LogInformation("Applying database migrations.");
        await db.Database.MigrateAsync(cancellationToken);
        await SeedData.EnsureSeededAsync(scope.ServiceProvider, cancellationToken);
        _logger.LogInformation("Database is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
