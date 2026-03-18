namespace ModularMonolith.Shared.Migrations;

public interface IMigratable
{
    Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}
