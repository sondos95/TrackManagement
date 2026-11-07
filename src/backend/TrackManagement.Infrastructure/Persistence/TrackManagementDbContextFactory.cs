using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrackManagement.Infrastructure.Persistence;

internal sealed class TrackManagementDbContextFactory : IDesignTimeDbContextFactory<TrackManagementDbContext>
{
    public TrackManagementDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TrackManagementDbContext>()
            .UseSqlite("Data Source=track-management.db;Foreign Keys=True")
            .Options;
        return new TrackManagementDbContext(options);
    }
}
