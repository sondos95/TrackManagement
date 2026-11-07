using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public sealed class TrackDistribution
{
    private DateTimeOffset _submittedAt;

    public int Id { get; set; }
    public int TrackId { get; set; }
    public int DspId { get; set; }
    public required DateTimeOffset SubmittedAt
    {
        get => _submittedAt;
        set => _submittedAt = value.ToUniversalTime();
    }

    public DistributionStatus Status { get; set; } = DistributionStatus.Pending;
}
