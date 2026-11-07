using Microsoft.EntityFrameworkCore;
using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Entities;
using TrackManagement.Infrastructure.Persistence;

namespace TrackManagement.Infrastructure.Repository;

internal sealed class ArtistRepository(TrackManagementDbContext db) : IArtistRepository
{
    public Task<Artist?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Artists.AsTracking().SingleOrDefaultAsync(artist => artist.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ArtistDto>> ListAsync(CancellationToken cancellationToken) =>
        await db.Artists.AsNoTracking()
            .OrderBy(artist => artist.Name)
            .ThenBy(artist => artist.Id)
            .Select(artist => new ArtistDto(artist.Id, artist.Name, artist.Email, artist.Country))
            .ToListAsync(cancellationToken);

    public void Add(Artist artist) => db.Artists.Add(artist);
}
