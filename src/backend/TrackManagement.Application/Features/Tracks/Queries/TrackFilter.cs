using TrackManagement.Domain.Enums;

namespace TrackManagement.Application.Features.Tracks.Queries;

public sealed record TrackFilter(int? ArtistId, string? Genre, TrackStatus? Status);
