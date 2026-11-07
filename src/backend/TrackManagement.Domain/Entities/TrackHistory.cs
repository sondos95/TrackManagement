using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public sealed class TrackHistory
{
    private int[] _assignedDspIds = [];

    private TrackHistory() { }

    public TrackHistory(int trackId, TrackHistoryAction action, string actorId, DateTimeOffset occurredAt,
        TrackStatus previousStatus, TrackStatus resultingStatus, IEnumerable<int> assignedDspIds)
    {
        TrackId = trackId;
        Action = action;
        ActorId = actorId;
        OccurredAt = occurredAt.ToUniversalTime();
        PreviousStatus = previousStatus;
        ResultingStatus = resultingStatus;
        _assignedDspIds = assignedDspIds.ToArray();
    }

    public int Id { get; private set; }
    public int TrackId { get; private set; }
    public TrackHistoryAction Action { get; private set; }
    public string ActorId { get; private set; } = null!;
    public DateTimeOffset OccurredAt { get; private set; }
    public TrackStatus PreviousStatus { get; private set; }
    public TrackStatus ResultingStatus { get; private set; }
    public IReadOnlyList<int> AssignedDspIds => Array.AsReadOnly(_assignedDspIds);
}
