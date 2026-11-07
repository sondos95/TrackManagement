using TrackManagement.Domain.Enums;

namespace TrackManagement.Application.Features.Tracks.DTOs.Responses;

public sealed record CreatedTrack(int Id, TrackStatus Status);
public sealed record TrackSummary(int Id, string Title, string ArtistName, string Genre, TrackStatus Status);
public sealed record DspAssignment(int DspId, string DspName, DateTimeOffset SubmittedAt, DistributionStatus Status);
public sealed record TrackDetails(int Id, string Title, int ArtistId, string ArtistName, string Isrc,
    DateOnly ReleaseDate, string Genre, TrackStatus Status, IReadOnlyList<DspAssignment> Assignments);
public sealed record TrackStatusResponse(int Id, TrackStatus Status);
