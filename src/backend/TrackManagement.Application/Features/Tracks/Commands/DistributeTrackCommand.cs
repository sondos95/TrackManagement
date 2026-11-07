using MediatR;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Interfaces;

namespace TrackManagement.Application.Features.Tracks.Commands;

public sealed record DistributeTrackCommand(int Id, IReadOnlyCollection<int>? DspIds)
    : IRequest<TrackResult<TrackDetails>>;

public sealed class DistributeTrackHandler(ITrackRepository tracks, IUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<DistributeTrackCommand, TrackResult<TrackDetails>>
{
    public async Task<TrackResult<TrackDetails>> Handle(DistributeTrackCommand request,
        CancellationToken cancellationToken)
    {
        var (id, dspIds) = request;
        if (id <= 0 || dspIds is null || dspIds.Count == 0 || dspIds.Any(dspId => dspId <= 0))
        {
            return TrackResult<TrackDetails>.Fail(TrackErrorKind.InvalidInput, "A positive track ID and at least one positive DSP ID are required.");
        }

        var requested = dspIds.Distinct().ToArray();
        var track = await tracks.GetByIdAsync(id, cancellationToken);
        if (track is null)
        {
            return TrackResult<TrackDetails>.Fail(TrackErrorKind.NotFound, "Track was not found.");
        }

        if (await tracks.CountExistingDspsAsync(requested, cancellationToken) != requested.Length)
        {
            return TrackResult<TrackDetails>.Fail(TrackErrorKind.NotFound, "One or more DSPs were not found.");
        }

        var submittedAt = clock.GetUtcNow();
        var changed = false;
        foreach (var dspId in requested)
        {
            changed |= track.AssignToDsp(dspId, submittedAt);
        }

        if (changed)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException exception)
            {
                var fresh = await tracks.GetDetailsAsync(id, cancellationToken);
                if (exception.Kind is PersistenceConflict.UniqueConstraint or PersistenceConflict.ConcurrentUpdate &&
                    fresh is not null && requested.All(dspId => fresh.Assignments.Any(item => item.DspId == dspId)))
                {
                    return TrackResult<TrackDetails>.Success(fresh);
                }

                return TrackResult<TrackDetails>.Fail(TrackErrorKind.Conflict, "Track or DSP assignments changed concurrently. Retry the submission.");
            }
        }

        var details = await tracks.GetDetailsAsync(id, cancellationToken);
        return details is null
            ? TrackResult<TrackDetails>.Fail(TrackErrorKind.NotFound, "Track was not found.")
            : TrackResult<TrackDetails>.Success(details);
    }
}
