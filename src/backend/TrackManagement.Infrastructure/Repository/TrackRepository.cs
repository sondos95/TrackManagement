using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Features.Tracks.Queries;
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

    public async Task<IReadOnlyList<TrackSummary>> ListAsync(TrackFilter filter, CancellationToken cancellationToken)
    {
        var tracks = db.Tracks.AsNoTracking();
        if (filter.ArtistId is { } artistId)
        {
            tracks = tracks.Where(track => track.ArtistId == artistId);
        }

        if (filter.Genre is { } genre)
        {
            tracks = tracks.Where(track => track.Genre.ToLower().Contains(genre.ToLower()));
        }

        if (filter.Status is { } status)
        {
            tracks = tracks.Where(track => track.Status == status);
        }

        return await (from track in tracks
                      join artist in db.Artists on track.ArtistId equals artist.Id
                      orderby track.Id
                      select new TrackSummary(track.Id, track.Title, artist.Name, track.Genre, track.Status))
            .ToListAsync(cancellationToken);
    }

    public Task<TrackDetails?> GetDetailsAsync(int id, CancellationToken cancellationToken) =>
        (from track in db.Tracks.AsNoTracking()
         join artist in db.Artists on track.ArtistId equals artist.Id
         where track.Id == id
         select new TrackDetails(track.Id, track.Title, track.ArtistId, artist.Name, track.Isrc,
             track.ReleaseDate, track.Genre, track.Status,
             track.Distributions.OrderBy(assignment => assignment.DspId)
                 .Select(assignment => new DspAssignment(assignment.DspId,
                     db.Dsps.Where(dsp => dsp.Id == assignment.DspId).Select(dsp => dsp.Name).First(),
                     assignment.SubmittedAt, assignment.Status))
                 .ToList()))
        .SingleOrDefaultAsync(cancellationToken);

    public Task<int> CountExistingDspsAsync(int[] distinctIds, CancellationToken cancellationToken) =>
        db.Dsps.CountAsync(dsp => distinctIds.Contains(dsp.Id), cancellationToken);
}
