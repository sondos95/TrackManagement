using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrackManagement.Api.DTOs.Requests;
using TrackManagement.Application.Features.Artists.Commands;
using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Application.Features.Artists.Queries;

namespace TrackManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/artists")]
public sealed class ArtistController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ArtistDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArtistDto>> Create(CreateArtistRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateArtistCommand(request.Name, request.Email, request.Country),
            cancellationToken);
        if (result.Artist is not { } artist)
        {
            return ValidationProblem(new ValidationProblemDetails(result.Errors)
            {
                Status = StatusCodes.Status400BadRequest
            });
        }

        return StatusCode(StatusCodes.Status201Created, artist);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ArtistDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArtistDto>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListArtistsQuery(), cancellationToken));
}
