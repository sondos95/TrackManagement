using TrackManagement.Domain.Enums;

namespace TrackManagement.Api.DTOs.Requests;

public sealed record UpdateTrackStatusRequest(TrackStatus? Status);
