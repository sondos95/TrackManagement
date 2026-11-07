using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public sealed class TrackDistribution
{
    internal TrackDistribution(int dspId, DateTimeOffset submittedAt)
    {
        DspId = dspId;
        SubmittedAt = submittedAt.ToUniversalTime();
    }

    public int Id { get; private set; }
    public int TrackId { get; private set; }
    public int DspId { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public DistributionStatus Status { get; private set; } = DistributionStatus.Pending;

    internal void SetStatus(DistributionStatus status) => Status = status;
}
