using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;
using Xunit;

namespace TrackManagement.UnitTests.Tracks;

public sealed class TrackTests
{
    private static readonly DateTimeOffset SubmittedAt = new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewTrackStartsInDraftWithoutDistributions()
    {
        var track = NewTrack();

        Assert.Equal(TrackStatus.Draft, track.Status);
        Assert.Empty(track.Distributions);
        Assert.Equal("USABC2600001", track.Isrc);
    }

    [Fact]
    public void FirstAssignmentSubmitsTrackAndStartsPending()
    {
        var track = NewTrack();

        var added = track.AssignToDsp(2, SubmittedAt);

        Assert.True(added);
        Assert.Equal(TrackStatus.Submitted, track.Status);
        Assert.Equal(DistributionStatus.Pending, Assert.Single(track.Distributions).Status);
    }

    [Fact]
    public void RepeatAssignmentPreservesTimestampDeliveryStatusAndRevision()
    {
        var track = NewTrack();
        track.AssignToDsp(2, SubmittedAt);
        track.UpdateDeliveryStatus(2, DistributionStatus.Live);
        var revision = track.Revision;

        var added = track.AssignToDsp(2, SubmittedAt.AddDays(1));

        Assert.False(added);
        Assert.Equal(revision, track.Revision);
        var assignment = Assert.Single(track.Distributions);
        Assert.True(assignment.SubmittedAt.EqualsExact(SubmittedAt));
        Assert.Equal(DistributionStatus.Live, assignment.Status);
    }

    [Fact]
    public void OperatorCanDistributeSubmittedTrackWithoutChangingDeliveryStatuses()
    {
        var track = NewTrack();
        track.AssignToDsp(2, SubmittedAt);
        track.AssignToDsp(3, SubmittedAt);
        track.UpdateDeliveryStatus(2, DistributionStatus.Live);
        track.UpdateDeliveryStatus(3, DistributionStatus.Rejected);

        var changed = track.ChangeStatus(TrackStatus.Distributed);
        track.AssignToDsp(4, SubmittedAt);

        Assert.True(changed);
        Assert.Equal(TrackStatus.Distributed, track.Status);
        Assert.Equal(new[] { DistributionStatus.Live, DistributionStatus.Rejected, DistributionStatus.Pending },
            track.Distributions.Select(assignment => assignment.Status));
    }

    [Theory]
    [InlineData(TrackStatus.Draft, TrackStatus.Submitted)]
    [InlineData(TrackStatus.Draft, TrackStatus.Distributed)]
    [InlineData(TrackStatus.Submitted, TrackStatus.Draft)]
    [InlineData(TrackStatus.Distributed, TrackStatus.Submitted)]
    [InlineData(TrackStatus.Distributed, TrackStatus.Draft)]
    public void InvalidTransitionLeavesStatusAndRevisionUnchanged(TrackStatus initial, TrackStatus requested)
    {
        var track = NewTrack();
        if (initial != TrackStatus.Draft)
        {
            track.AssignToDsp(2, SubmittedAt);
        }

        if (initial == TrackStatus.Distributed)
        {
            track.ChangeStatus(initial);
        }

        var revision = track.Revision;

        Assert.Throws<InvalidOperationException>(() => track.ChangeStatus(requested));
        Assert.Equal(initial, track.Status);
        Assert.Equal(revision, track.Revision);
    }

    [Fact]
    public void SettingCurrentStatusIsANoOp()
    {
        var track = NewTrack();
        track.AssignToDsp(2, SubmittedAt);
        var revision = track.Revision;

        var changed = track.ChangeStatus(TrackStatus.Submitted);

        Assert.False(changed);
        Assert.Equal(revision, track.Revision);
    }

    private static Track NewTrack() => new("First Track", 1, " usabc2600001 ", new DateOnly(2026, 5, 15), "Jazz");
}
