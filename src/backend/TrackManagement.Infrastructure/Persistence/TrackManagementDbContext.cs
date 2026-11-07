using Microsoft.EntityFrameworkCore;
using TrackManagement.Domain.Entities;
using TrackManagement.Infrastructure.Persistence.Configurations;

namespace TrackManagement.Infrastructure.Persistence;

public sealed class TrackManagementDbContext(DbContextOptions<TrackManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Dsp> Dsps => Set<Dsp>();
    public DbSet<TrackDistribution> TrackDistributions => Set<TrackDistribution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArtistConfiguration());
        modelBuilder.ApplyConfiguration(new TrackConfiguration());
        modelBuilder.ApplyConfiguration(new DspConfiguration());
        modelBuilder.ApplyConfiguration(new TrackDistributionConfiguration());
    }
}
