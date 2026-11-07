using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Application.Interfaces;

public interface IArtistRepository
{
    Task<Artist?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtistDto>> ListAsync(CancellationToken cancellationToken);
    void Add(Artist artist);
}
