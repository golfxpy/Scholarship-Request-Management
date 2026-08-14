using Microsoft.EntityFrameworkCore;

namespace ScholarshipRequest.Api.Data;

public static class DatabaseMigrator
{
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
