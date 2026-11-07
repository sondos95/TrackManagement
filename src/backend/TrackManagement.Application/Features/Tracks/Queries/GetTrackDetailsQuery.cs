using MediatR;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Interfaces;

namespace TrackManagement.Application.Features.Tracks.Queries;

public sealed record GetTrackDetailsQuery(int Id) : IRequest<TrackResult<TrackDetails>>;

internal sealed class GetTrackDetailsHandler(ITrackRepository tracks)
    : IRequestHandler<GetTrackDetailsQuery, TrackResult<TrackDetails>>
{
    public async Task<TrackResult<TrackDetails>> Handle(GetTrackDetailsQuery request, CancellationToken cancellationToken)
    {
        var id = request.Id;
        if (id <= 0)
        {
            return TrackResult<TrackDetails>.Fail(TrackErrorKind.InvalidInput, "Track ID must be positive.");
        }

        var track = await tracks.GetDetailsAsync(id, cancellationToken);
        return track is null
            ? TrackResult<TrackDetails>.Fail(TrackErrorKind.NotFound, "Track was not found.")
            : TrackResult<TrackDetails>.Success(track);
    }
}
