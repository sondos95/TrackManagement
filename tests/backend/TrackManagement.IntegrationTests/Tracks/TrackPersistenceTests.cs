using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;
using TrackManagement.Infrastructure.Persistence;
using Xunit;

namespace TrackManagement.IntegrationTests.Tracks;

public sealed class TrackPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
    private DbContextOptions<TrackManagementDbContext> _options = null!;
    private int _artistId;
    private int _dspId;
    private int _trackId;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<TrackManagementDbContext>().UseSqlite(_connection).Options;
        await using var db = new TrackManagementDbContext(_options);
        await db.Database.MigrateAsync();

        var artist = new Artist { Name = "Artist", Email = "artist@example.com", Country = "EG" };
        var dsp = new Dsp { Name = "DSP" };
        db.AddRange(artist, dsp);
        await db.SaveChangesAsync();
        _artistId = artist.Id;
        _dspId = dsp.Id;

        var track = NewTrack("USABC2600001");
        db.Tracks.Add(track);
        await db.SaveChangesAsync();
        _trackId = track.Id;
    }

    [Fact]
    public async Task RemovingHistoryPreservesExistingAssessmentRows()
    {
        await using var db = new TrackManagementDbContext(_options);
        await db.Database.MigrateAsync("20261107044720_InitialTrackManagement");
        var track = await db.Tracks.SingleAsync();
        track.AssignToDsp(_dspId, Now);
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO TrackHistory
                (TrackId, Action, ActorId, OccurredAt, PreviousStatus, ResultingStatus, AssignedDspIds)
            VALUES ({_trackId}, 'DspAssigned', 'operator', {Now}, 'Draft', 'Submitted', '[]')
            """);

        await db.Database.MigrateAsync();

        Assert.Equal(0, await db.Database.SqlQueryRaw<int>(
            """SELECT COUNT(*) AS "Value" FROM sqlite_master WHERE name IN ('TrackHistory', 'IX_TrackHistory_TrackId')""")
            .SingleAsync());
        Assert.Equal(_artistId, (await db.Artists.AsNoTracking().SingleAsync()).Id);
        Assert.Equal(_dspId, (await db.Dsps.AsNoTracking().SingleAsync()).Id);
        Assert.Equal(TrackStatus.Submitted, (await db.Tracks.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(_dspId, (await db.TrackDistributions.AsNoTracking().SingleAsync()).DspId);
    }

    [Fact]
    public async Task SqliteRejectsDuplicateNormalizedIsrc()
    {
        await using var db = new TrackManagementDbContext(_options);
        db.Tracks.Add(NewTrack(" usabc2600001 "));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(2067, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        Assert.Equal(1, await db.Tracks.CountAsync());
    }

    [Fact]
    public async Task SqliteRejectsDuplicateDspAssignment()
    {
        await using var db = new TrackManagementDbContext(_options);
        var track = await db.Tracks.SingleAsync();
        track.AssignToDsp(_dspId, Now);
        await db.SaveChangesAsync();
        const string pending = "Pending";

        var error = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO TrackDistributions (TrackId, DspId, SubmittedAt, Status) VALUES ({_trackId}, {_dspId}, {Now}, {pending})"));

        Assert.Equal(2067, error.SqliteExtendedErrorCode);
        Assert.Equal(1, await db.TrackDistributions.CountAsync());
    }

    [Fact]
    public async Task FailedSaveRollsBackTrackAndAssignments()
    {
        await using var db = new TrackManagementDbContext(_options);
        var track = await db.Tracks.SingleAsync();
        track.AssignToDsp(_dspId, Now);
        const int missingDspId = int.MaxValue;
        track.AssignToDsp(missingDspId, Now);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(787, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
        await using var verification = new TrackManagementDbContext(_options);
        var unchanged = await verification.Tracks.Include(item => item.Distributions).SingleAsync();
        Assert.Equal(TrackStatus.Draft, unchanged.Status);
        Assert.Equal(0L, unchanged.Revision);
        Assert.Empty(unchanged.Distributions);
    }

    private Track NewTrack(string isrc) => new("Track", _artistId, isrc, new DateOnly(2026, 5, 15), "Jazz");

    public async Task DisposeAsync() => await _connection.DisposeAsync();
}
