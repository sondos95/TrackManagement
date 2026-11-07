using MediatR;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Interfaces;

namespace TrackManagement.Application.Features.Tracks.Queries;

public sealed record ListTracksQuery(TrackFilter Filter) : IRequest<TrackResult<IReadOnlyList<TrackSummary>>>;

internal sealed class ListTracksHandler(ITrackRepository tracks)
    : IRequestHandler<ListTracksQuery, TrackResult<IReadOnlyList<TrackSummary>>>
{
    public async Task<TrackResult<IReadOnlyList<TrackSummary>>> Handle(
        ListTracksQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var genre = filter.Genre?.Trim();
        if (filter.ArtistId is <= 0 || genre is { Length: 0 or > 100 } ||
            (filter.Status is { } status && !Enum.IsDefined(status)))
        {
            return TrackResult<IReadOnlyList<TrackSummary>>.Fail(TrackErrorKind.InvalidInput, "Track filter is invalid.");
        }

        var items = await tracks.ListAsync(filter with { Genre = genre }, cancellationToken);
        return TrackResult<IReadOnlyList<TrackSummary>>.Success(items);
    }
}
