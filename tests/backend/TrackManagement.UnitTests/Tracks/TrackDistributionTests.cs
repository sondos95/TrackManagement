using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;
using Xunit;

namespace TrackManagement.UnitTests.Tracks;

public sealed class TrackDistributionTests
{
    [Fact]
    public void NewDistributionStartsPending()
    {
        var submittedAt = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);

        var distribution = new TrackDistribution { TrackId = 1, DspId = 2, SubmittedAt = submittedAt };

        Assert.Equal(DistributionStatus.Pending, distribution.Status);
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(-4, 16)]
    public void SubmissionTimestampUsesUtcAndPreservesTheInstant(int offsetHours, int expectedUtcHour)
    {
        var submittedAt = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.FromHours(offsetHours));
        var expectedUtc = new DateTimeOffset(2026, 5, 15, expectedUtcHour, 0, 0, TimeSpan.Zero);

        var distribution = new TrackDistribution { TrackId = 1, DspId = 2, SubmittedAt = submittedAt };

        Assert.Equal(expectedUtc, distribution.SubmittedAt);
        Assert.Equal(TimeSpan.Zero, distribution.SubmittedAt.Offset);
    }
}
