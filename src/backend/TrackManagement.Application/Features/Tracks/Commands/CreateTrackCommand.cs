using MediatR;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Application.Features.Tracks.Commands;

public sealed record CreateTrackCommand(string? Title, int ArtistId, string? Isrc, DateOnly ReleaseDate, string? Genre)
    : IRequest<TrackResult<CreatedTrack>>;

internal sealed class CreateTrackHandler(IArtistRepository artists, ITrackRepository tracks, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTrackCommand, TrackResult<CreatedTrack>>
{
    public async Task<TrackResult<CreatedTrack>> Handle(CreateTrackCommand input, CancellationToken cancellationToken)
    {
        var title = input.Title?.Trim();
        var genre = input.Genre?.Trim();
        var isrc = input.Isrc is null ? null : Track.NormalizeIsrc(input.Isrc);
        if (string.IsNullOrEmpty(title) || title.Length > 200 ||
            string.IsNullOrEmpty(genre) || genre.Length > 100 ||
            input.ArtistId <= 0 || input.ReleaseDate == default || !IsValidIsrc(isrc))
        {
            return TrackResult<CreatedTrack>.Fail(TrackErrorKind.InvalidInput, "Track fields or ISRC are invalid.");
        }

        if (await artists.GetByIdAsync(input.ArtistId, cancellationToken) is null)
        {
            return TrackResult<CreatedTrack>.Fail(TrackErrorKind.NotFound, "Artist was not found.");
        }

        var track = new Track(title, input.ArtistId, isrc!, input.ReleaseDate, genre);
        tracks.Add(track);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException exception)
        {
            return exception.Kind == PersistenceConflict.UniqueConstraint
                ? TrackResult<CreatedTrack>.Fail(TrackErrorKind.Conflict, "ISRC is already in use.")
                : TrackResult<CreatedTrack>.Fail(TrackErrorKind.Conflict, "The artist or track data changed during creation.");
        }

        return TrackResult<CreatedTrack>.Success(new(track.Id, track.Status));
    }

    private static bool IsValidIsrc(string? isrc) =>
        isrc is { Length: 12 } &&
        isrc[..2].All(char.IsAsciiLetterUpper) &&
        isrc[2..5].All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character)) &&
        isrc[5..].All(char.IsAsciiDigit);
}
