using ModularMonolith.Shared.Migrations;

namespace ModularMonolith;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateAllDatabasesAsync(this WebApplication app)
    {
        var migratables = app.Services.GetServices<IMigratable>();

        foreach (var migratable in migratables)
        {
            await migratable.MigrateAsync(app.Services);
        }
    }
}
