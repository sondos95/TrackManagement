namespace TrackManagement.Domain.Entities;

public sealed class Artist
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Country { get; set; }

    public ICollection<Track> Tracks { get; } = new List<Track>();
}
