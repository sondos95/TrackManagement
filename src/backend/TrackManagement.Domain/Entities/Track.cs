using TrackManagement.Domain.Enums;

namespace TrackManagement.Domain.Entities;

public sealed class Track
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int ArtistId { get; set; }
    public required string Isrc { get; set; }
    public DateOnly ReleaseDate { get; set; }
    public required string Genre { get; set; }
    public TrackStatus Status { get; set; } = TrackStatus.Draft;

    public ICollection<TrackDistribution> Distributions { get; } = new List<TrackDistribution>();
}
