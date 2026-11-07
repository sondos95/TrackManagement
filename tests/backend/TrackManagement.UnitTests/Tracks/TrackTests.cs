using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;
using Xunit;

namespace TrackManagement.UnitTests.Tracks;

public sealed class TrackTests
{
    [Fact]
    public void NewTrackStartsInDraftWithoutDistributions()
    {
        var releaseDate = new DateOnly(2026, 5, 15);

        var track = new Track
        {
            Title = "First Track",
            ArtistId = 1,
            Isrc = "USABC2600001",
            ReleaseDate = releaseDate,
            Genre = "Jazz"
        };

        Assert.Equal(TrackStatus.Draft, track.Status);
        Assert.Empty(track.Distributions);
    }
}
