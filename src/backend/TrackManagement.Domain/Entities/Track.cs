using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public sealed class Track
{
    private readonly List<TrackDistribution> _distributions = [];

    public Track(string title, int artistId, string isrc, DateOnly releaseDate, string genre)
    {
        Title = title;
        ArtistId = artistId;
        Isrc = NormalizeIsrc(isrc);
        ReleaseDate = releaseDate;
        Genre = genre;
    }

    public int Id { get; private set; }
    public string Title { get; }
    public int ArtistId { get; }
    public string Isrc { get; }
    public DateOnly ReleaseDate { get; }
    public string Genre { get; }
    public TrackStatus Status { get; private set; } = TrackStatus.Draft;
    public long Revision { get; private set; }
    public IReadOnlyCollection<TrackDistribution> Distributions => _distributions.AsReadOnly();

    public static string NormalizeIsrc(string isrc) => isrc.Trim().ToUpperInvariant();

    public bool AssignToDsp(int dspId, DateTimeOffset submittedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dspId);
        if (_distributions.Any(assignment => assignment.DspId == dspId))
        {
            return false;
        }

        _distributions.Add(new TrackDistribution(dspId, submittedAt));
        if (Status == TrackStatus.Draft)
        {
            Status = TrackStatus.Submitted;
        }

        Revision++;
        return true;
    }

    public bool CanChangeStatusTo(TrackStatus status) =>
        Status == status ||
        (Status == TrackStatus.Submitted && status == TrackStatus.Distributed && _distributions.Count > 0);

    public bool ChangeStatus(TrackStatus status)
    {
        if (!CanChangeStatusTo(status))
        {
            throw new InvalidOperationException($"Cannot change track status from {Status} to {status}.");
        }

        if (Status == status)
        {
            return false;
        }

        Status = status;
        Revision++;
        return true;
    }

    public void UpdateDeliveryStatus(int dspId, DistributionStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        var assignment = _distributions.Single(item => item.DspId == dspId);
        if (assignment.Status != status)
        {
            assignment.SetStatus(status);
            Revision++;
        }
    }
}
