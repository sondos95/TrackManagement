namespace TrackManagement.Api.DTOs.Requests;

public sealed record CreateTrackRequest(string? Title, int ArtistId, string? Isrc, DateOnly ReleaseDate, string? Genre);
