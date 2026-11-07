using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Entities;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Infrastructure.Repository;

internal sealed class TrackRepository(TrackManagementDbContext db) : ITrackRepository
{
    public Task<Track?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Tracks.AsTracking().Include(track => track.Distributions)
            .SingleOrDefaultAsync(track => track.Id == id, cancellationToken);

    public void Add(Track track) => db.Tracks.Add(track);
}
