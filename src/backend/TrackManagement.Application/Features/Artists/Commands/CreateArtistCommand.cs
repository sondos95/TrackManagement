using MediatR;
using System.Net.Mail;
using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Application.Features.Artists.Commands;

public sealed record CreateArtistCommand(string? Name, string? Email, string? Country) : IRequest<CreateArtistResult>;

public sealed record CreateArtistResult(ArtistDto? Artist, IDictionary<string, string[]> Errors);

internal sealed class CreateArtistHandler(IArtistRepository artists, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateArtistCommand, CreateArtistResult>
{
    public async Task<CreateArtistResult> Handle(CreateArtistCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var country = request.Country?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();

        ValidateRequired(name, Artist.NameMaxLength, "name", errors);
        ValidateRequired(email, Artist.EmailMaxLength, "email", errors);
        ValidateRequired(country, Artist.CountryMaxLength, "country", errors);
        if (!errors.ContainsKey("email") &&
            (!MailAddress.TryCreate(email, out var address) || address.Address != email))
        {
            errors["email"] = ["Enter a valid email address."];
        }

        if (errors.Count > 0)
        {
            return new(null, errors);
        }

        var artist = new Artist { Name = name, Email = email, Country = country };
        artists.Add(artist);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(new ArtistDto(artist.Id, artist.Name, artist.Email, artist.Country), errors);
    }

    private static void ValidateRequired(string value, int maxLength, string field,
        Dictionary<string, string[]> errors)
    {
        if (value.Length == 0)
        {
            errors[field] = ["This field is required."];
        }
        else if (value.Length > maxLength)
        {
            errors[field] = [$"Use at most {maxLength} characters."];
        }
    }
}
