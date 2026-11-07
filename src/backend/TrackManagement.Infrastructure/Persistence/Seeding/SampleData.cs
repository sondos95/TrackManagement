using Microsoft.EntityFrameworkCore;
using TrackManagement.Domain.Entities;
using TrackManagement.Domain.Enums;

namespace TrackManagement.Infrastructure.Persistence.Seeding;

internal static class SampleData
{
    private const int FirstSampleId = 10_001;
    private static readonly DateTimeOffset FirstSubmission = new(2025, 5, 1, 10, 0, 0, TimeSpan.Zero);

    private static readonly (string Name, string Email, string Country)[] Artists =
    [
        ("Nora Vale", "nora.vale@example.com", "Canada"),
        ("Atlas Harbor", "atlas.harbor@example.com", "United Kingdom"),
        ("Mina Sayegh", "mina.sayegh@example.com", "Egypt")
    ];

    private static readonly string[] DspNames = ["Spotify", "Apple Music", "YouTube"];

    private static readonly SampleTrack[] Tracks =
    [
        new("Paper Satellites", 0, "Indie Pop", TrackStatus.Draft, []),
        new("Copper Rain", 1, "Alternative Rock", TrackStatus.Draft, []),
        new("A Window in Cairo", 2, "Jazz", TrackStatus.Draft, []),
        new("Coastline Static", 0, "Electronic", TrackStatus.Submitted, [(0, DistributionStatus.Pending)]),
        new("Lantern Signal", 1, "Alternative Rock", TrackStatus.Submitted,
            [(1, DistributionStatus.Pending), (2, DistributionStatus.Rejected)]),
        new("Blue Hour Sketch", 2, "Jazz", TrackStatus.Submitted, [(2, DistributionStatus.Pending)]),
        new("Borrowed Constellations", 0, "Indie Pop", TrackStatus.Distributed,
            [(0, DistributionStatus.Live), (1, DistributionStatus.Live)]),
        new("The Long Way Home", 2, "Soul", TrackStatus.Distributed,
            [(0, DistributionStatus.Pending), (1, DistributionStatus.Live), (2, DistributionStatus.Rejected)])
    ];

    internal static void Seed(TrackManagementDbContext db)
    {
        var artists = FindArtists(db).ToDictionary(artist => artist.Id);
        var dsps = FindDsps(db).ToDictionary(dsp => dsp.Id);
        var tracks = FindTracks(db).ToDictionary(track => track.Id);
        StageMissingRows(db, artists, dsps, tracks);
        db.SaveChanges();
    }

    internal static async Task SeedAsync(TrackManagementDbContext db, CancellationToken cancellationToken)
    {
        var artists = await FindArtists(db).ToDictionaryAsync(artist => artist.Id, cancellationToken);
        var dsps = await FindDsps(db).ToDictionaryAsync(dsp => dsp.Id, cancellationToken);
        var tracks = await FindTracks(db).ToDictionaryAsync(track => track.Id, cancellationToken);
        StageMissingRows(db, artists, dsps, tracks);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Artist> FindArtists(TrackManagementDbContext db) =>
        db.Artists.AsNoTracking().Where(artist => artist.Id >= FirstSampleId && artist.Id < FirstSampleId + Artists.Length);

    private static IQueryable<Dsp> FindDsps(TrackManagementDbContext db) =>
        db.Dsps.AsNoTracking().Where(dsp => dsp.Id >= FirstSampleId && dsp.Id < FirstSampleId + DspNames.Length);

    private static IQueryable<Track> FindTracks(TrackManagementDbContext db) =>
        db.Tracks.AsNoTracking().Where(track => track.Id >= FirstSampleId && track.Id < FirstSampleId + Tracks.Length);

    private static void StageMissingRows(TrackManagementDbContext db, Dictionary<int, Artist> artists,
        Dictionary<int, Dsp> dsps, Dictionary<int, Track> tracks)
    {
        for (var index = 0; index < Artists.Length; index++)
        {
            var id = FirstSampleId + index;
            var (name, email, country) = Artists[index];
            if (!artists.TryGetValue(id, out var existing))
            {
                db.Artists.Add(new Artist { Id = id, Name = name, Email = email, Country = country });
            }
            else if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Sample artist ID {id} is already used by a different artist.");
            }
        }

        for (var index = 0; index < DspNames.Length; index++)
        {
            var id = FirstSampleId + index;
            var name = DspNames[index];
            if (!dsps.TryGetValue(id, out var existing))
            {
                db.Dsps.Add(new Dsp { Id = id, Name = name });
            }
            else if (!string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Sample DSP ID {id} is already used by a different DSP.");
            }
        }

        for (var index = 0; index < Tracks.Length; index++)
        {
            var id = FirstSampleId + index;
            var isrc = FormattableString.Invariant($"QZTMG25{index + 1:00000}");
            if (tracks.TryGetValue(id, out var existing))
            {
                if (existing.Isrc != isrc)
                {
                    throw new InvalidOperationException($"Sample track ID {id} is already used by a different track.");
                }

                continue;
            }

            var sample = Tracks[index];
            var track = new Track(sample.Title, FirstSampleId + sample.ArtistIndex, isrc,
                new DateOnly(2025, 5, 10).AddDays(index), sample.Genre);
            db.Entry(track).Property(item => item.Id).CurrentValue = id;

            foreach (var (dspIndex, status) in sample.Assignments)
            {
                var dspId = FirstSampleId + dspIndex;
                track.AssignToDsp(dspId, FirstSubmission.AddDays(index));
                track.UpdateDeliveryStatus(dspId, status);
            }

            foreach (var assignment in track.Distributions)
            {
                var assignmentId = assignment.DspId + index * DspNames.Length;
                db.Entry(assignment).Property(item => item.Id).CurrentValue = assignmentId;
            }

            track.ChangeStatus(sample.Status);
            db.Tracks.Add(track);
        }
    }

    private sealed record SampleTrack(string Title, int ArtistIndex, string Genre, TrackStatus Status,
        (int DspIndex, DistributionStatus Status)[] Assignments);
}
