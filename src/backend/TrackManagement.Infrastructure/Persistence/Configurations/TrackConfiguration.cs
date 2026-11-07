using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrackManagement.Domain.Entities;

namespace TrackManagement.Infrastructure.Persistence.Configurations;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.HasKey(track => track.Id);
        builder.Property(track => track.Title).IsRequired().HasMaxLength(200);
        builder.Property(track => track.Isrc).IsRequired().HasMaxLength(12);
        builder.Property(track => track.ReleaseDate).IsRequired();
        builder.Property(track => track.Genre).IsRequired().HasMaxLength(100);
        builder.Property(track => track.Status).HasConversion<string>().IsRequired().HasMaxLength(16);

        builder.Property<string>("NormalizedIsrc")
            .HasComputedColumnSql("upper(trim(\"Isrc\"))", stored: true)
            .IsRequired()
            .HasMaxLength(12);
        builder.HasIndex("NormalizedIsrc").IsUnique();

        builder.HasOne<Artist>()
            .WithMany(artist => artist.Tracks)
            .HasForeignKey(track => track.ArtistId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
