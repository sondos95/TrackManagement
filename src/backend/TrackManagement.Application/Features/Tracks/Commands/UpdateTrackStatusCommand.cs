using MediatR;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Enums;

namespace TrackManagement.Application.Features.Tracks.Commands;

public sealed record UpdateTrackStatusCommand(int Id, TrackStatus? Status) : IRequest<TrackResult<TrackStatusResponse>>;

internal sealed class UpdateTrackStatusHandler(ITrackRepository tracks, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateTrackStatusCommand, TrackResult<TrackStatusResponse>>
{
    public async Task<TrackResult<TrackStatusResponse>> Handle(UpdateTrackStatusCommand request,
        CancellationToken cancellationToken)
    {
        var (id, status) = request;
        if (id <= 0 || status is not { } desired || !Enum.IsDefined(desired))
        {
            return TrackResult<TrackStatusResponse>.Fail(TrackErrorKind.InvalidInput, "Track ID or status is invalid.");
        }

        var track = await tracks.GetByIdAsync(id, cancellationToken);
        if (track is null)
        {
            return TrackResult<TrackStatusResponse>.Fail(TrackErrorKind.NotFound, "Track was not found.");
        }

        if (!track.CanChangeStatusTo(desired))
        {
            return TrackResult<TrackStatusResponse>.Fail(TrackErrorKind.Conflict, $"Cannot change track status from {track.Status} to {desired}.");
        }

        if (track.ChangeStatus(desired))
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (PersistenceConflictException)
            {
                var fresh = await tracks.GetByIdAsync(id, cancellationToken);
                if (fresh?.Status != desired)
                {
                    return TrackResult<TrackStatusResponse>.Fail(TrackErrorKind.Conflict, "Track status changed concurrently.");
                }
            }
        }

        return TrackResult<TrackStatusResponse>.Success(new(id, desired));
    }
}
