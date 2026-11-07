using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TrackManagement.Infrastructure.Persistence.Seeding;

namespace TrackManagement.Infrastructure.Persistence;

internal sealed class TrackManagementDbContextFactory : IDesignTimeDbContextFactory<TrackManagementDbContext>
{
    public TrackManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TrackManagementDbContext>()
            .UseSqlite("Data Source=track-management.db;Foreign Keys=True");

        if (args.Contains("--seed-sample-data", StringComparer.Ordinal))
        {
            options.UseSeeding((db, _) => SampleData.Seed((TrackManagementDbContext)db));
            options.UseAsyncSeeding((db, _, cancellationToken) =>
                SampleData.SeedAsync((TrackManagementDbContext)db, cancellationToken));
        }

        return new TrackManagementDbContext(options.Options);
    }
}
