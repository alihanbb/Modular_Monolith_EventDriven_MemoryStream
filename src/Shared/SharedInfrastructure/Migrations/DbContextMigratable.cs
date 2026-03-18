using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolith.Shared.Migrations;

namespace ModularMonolith.Shared.Infrastructure.Migrations;

public sealed class DbContextMigratable<TDbContext> : IMigratable
    where TDbContext : DbContext
{
    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
