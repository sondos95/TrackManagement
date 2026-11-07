namespace TrackManagement.Api.DTOs.Requests;

public sealed record CreateArtistRequest(string? Name, string? Email, string? Country);
