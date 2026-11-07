using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Features.Tracks.Queries;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Application.Interfaces;

public interface ITrackRepository
{
    Task<Track?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    void Add(Track track);
    Task<IReadOnlyList<TrackSummary>> ListAsync(TrackFilter filter, CancellationToken cancellationToken);
    Task<TrackDetails?> GetDetailsAsync(int id, CancellationToken cancellationToken);
    Task<int> CountExistingDspsAsync(int[] distinctIds, CancellationToken cancellationToken);
}
