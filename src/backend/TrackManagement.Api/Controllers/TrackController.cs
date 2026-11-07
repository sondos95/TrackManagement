using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackManagement.Api.DTOs.Requests;
using TrackManagement.Application.Features.Tracks;
using TrackManagement.Application.Features.Tracks.Commands;
using TrackManagement.Application.Features.Tracks.DTOs.Responses;
using TrackManagement.Application.Features.Tracks.Queries;
using TrackManagement.Domain.Enums;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tracks")]
public sealed class TrackController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatedTrack>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedTrack>> Create(CreateTrackRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTrackCommand(request.Title, request.ArtistId, request.Isrc,
            request.ReleaseDate, request.Genre);
        var result = await sender.Send(command, cancellationToken);
        return result.Error is { } error
            ? MapError(error)
            : CreatedAtAction(nameof(GetDetails), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrackSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TrackSummary>>> List(
        CancellationToken cancellationToken, [FromQuery] int? artistId, [FromQuery] string? genre,
        [FromQuery] string? status)
    {
        TrackStatus? filterStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<TrackStatus>(status, ignoreCase: true, out var parsed) ||
                !string.Equals(status, Enum.GetName(parsed), StringComparison.OrdinalIgnoreCase))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Track status filter is invalid.");
            }

            filterStatus = parsed;
        }

        var query = new ListTracksQuery(new(artistId, genre, filterStatus));
        return Map(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id}")]
    [ProducesResponseType<TrackDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackDetails>> GetDetails(int id, CancellationToken cancellationToken) =>
        Map(await sender.Send(new GetTrackDetailsQuery(id), cancellationToken));

    [HttpPost("{id}/distribute")]
    [ProducesResponseType<TrackDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackDetails>> Distribute(int id, DistributeTrackRequest request,
        CancellationToken cancellationToken) =>
        Map(await sender.Send(new DistributeTrackCommand(id, request.DspIds), cancellationToken));

    [HttpPatch("{id}/status")]
    [ProducesResponseType<TrackStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrackStatusResponse>> UpdateStatus(int id, UpdateTrackStatusRequest request,
        CancellationToken cancellationToken) =>
        Map(await sender.Send(new UpdateTrackStatusCommand(id, request.Status), cancellationToken));

    private ActionResult<T> Map<T>(TrackResult<T> result) where T : class =>
        result.Error is { } error ? MapError(error) : Ok(result.Value);

    private ObjectResult MapError(TrackError error) => Problem(detail: error.Message, statusCode: error.Kind switch
    {
        TrackErrorKind.InvalidInput => StatusCodes.Status400BadRequest,
        TrackErrorKind.NotFound => StatusCodes.Status404NotFound,
        TrackErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => throw new ArgumentOutOfRangeException(nameof(error))
    });
}
