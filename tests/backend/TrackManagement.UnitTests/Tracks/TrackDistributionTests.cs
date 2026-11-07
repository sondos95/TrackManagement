using TrackManagement.Domain.Entities;
using Xunit;

namespace TrackManagement.UnitTests.Tracks;

public sealed class TrackDistributionTests
{
    [Theory]
    [InlineData(2, 10)]
    [InlineData(-4, 16)]
    public void SubmissionTimestampUsesUtcAndPreservesTheInstant(int offsetHours, int expectedUtcHour)
    {
        var submittedAt = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.FromHours(offsetHours));
        var expectedUtc = new DateTimeOffset(2026, 5, 15, expectedUtcHour, 0, 0, TimeSpan.Zero);
        var track = new Track("First Track", 1, "USABC2600001", new DateOnly(2026, 5, 15), "Jazz");

        track.AssignToDsp(2, submittedAt);

        var distribution = Assert.Single(track.Distributions);
        Assert.Equal(expectedUtc, distribution.SubmittedAt);
        Assert.Equal(TimeSpan.Zero, distribution.SubmittedAt.Offset);
    }
}
