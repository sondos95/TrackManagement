using MediatR;
using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Application.Interfaces;

namespace TrackManagement.Application.Features.Artists.Queries;

public sealed record ListArtistsQuery : IRequest<IReadOnlyList<ArtistDto>>;

internal sealed class ListArtistsHandler(IArtistRepository artists)
    : IRequestHandler<ListArtistsQuery, IReadOnlyList<ArtistDto>>
{
    public Task<IReadOnlyList<ArtistDto>> Handle(ListArtistsQuery request, CancellationToken cancellationToken) =>
        artists.ListAsync(cancellationToken);
}
