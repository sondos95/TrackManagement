using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(artist => artist.Id);
        builder.Property(artist => artist.Name).IsRequired().HasMaxLength(200);
        builder.Property(artist => artist.Email).IsRequired().HasMaxLength(254);
        builder.Property(artist => artist.Country).IsRequired().HasMaxLength(100);
    }
}
