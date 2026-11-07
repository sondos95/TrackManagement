using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(artist => artist.Id);
        builder.Property(artist => artist.Name).IsRequired().HasMaxLength(Artist.NameMaxLength);
        builder.Property(artist => artist.Email).IsRequired().HasMaxLength(Artist.EmailMaxLength);
        builder.Property(artist => artist.Country).IsRequired().HasMaxLength(Artist.CountryMaxLength);
    }
}
