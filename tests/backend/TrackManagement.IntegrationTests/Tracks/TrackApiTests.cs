using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TrackManagement.Application.Features.Tracks.Commands;
using TrackManagement.Application.Interfaces;
using TrackManagement.Domain.Entities;
using TrackManagement.Infrastructure.Persistence;
using Xunit;

namespace TrackManagement.IntegrationTests.Tracks;

public sealed class TrackApiTests : IAsyncLifetime
{
    private const string Issuer = "track-integration";
    private const string Audience = "track-api";
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    private readonly string _connectionString =
        $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;Foreign Keys=True;Pooling=False";
    private SqliteConnection _anchor = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private int _artist;
    private int _otherArtist;
    private int _dsp;

    public async Task InitializeAsync()
    {
        _anchor = new SqliteConnection(_connectionString);
        await _anchor.OpenAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TrackManagement"] = _connectionString,
                    ["Authentication:Schemes:Bearer:ValidIssuer"] = Issuer,
                    ["Authentication:Schemes:Bearer:ValidAudiences:0"] = Audience,
                    ["Authentication:Schemes:Bearer:SigningKeys:0:Issuer"] = Issuer,
                    ["Authentication:Schemes:Bearer:SigningKeys:0:Value"] = Convert.ToBase64String(_key)
                }));
        });
        _client = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrackManagementDbContext>();
        await db.Database.MigrateAsync();
        var artist = new Artist { Name = "Test Artist", Email = "artist@example.com", Country = "EG" };
        var otherArtist = new Artist { Name = "Other Artist", Email = "other@example.com", Country = "EG" };
        var dsp = new Dsp { Name = "Test DSP" };
        db.AddRange(artist, otherArtist, dsp);
        await db.SaveChangesAsync();
        (_artist, _otherArtist, _dsp) = (artist.Id, otherArtist.Id, dsp.Id);
    }

    [Theory]
    [InlineData("POST", "/api/tracks")]
    [InlineData("GET", "/api/tracks")]
    [InlineData("GET", "/api/tracks/1")]
    [InlineData("POST", "/api/tracks/1/distribute")]
    [InlineData("PATCH", "/api/tracks/1/status")]
    public async Task EachTrackRouteRejectsAnonymousRequests(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("signature")]
    [InlineData("expiration")]
    public async Task JwtValidationRejectsInvalidTokens(string invalidField)
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken(invalidField));
        using var response = await _client.GetAsync("/api/tracks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DemoTokenCanReadTracksAndCannotWrite()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var id = await CreateTrackAsync(_artist, "USABC2600010", "Jazz");
        _client.DefaultRequestHeaders.Authorization = null;

        using var demoRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/demo-token");
        using var issuance = await _client.SendAsync(demoRequest);
        Assert.Equal(HttpStatusCode.OK, issuance.StatusCode);
        Assert.True(issuance.Headers.CacheControl?.NoStore);
        var body = await issuance.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new("Bearer", body.GetProperty("accessToken").GetString());

        using var list = await _client.GetAsync("/api/tracks");
        using var detail = await _client.GetAsync($"/api/tracks/{id}");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);

        foreach (var (method, path) in new[]
        {
            (HttpMethod.Post, "/api/artists"),
            (HttpMethod.Post, "/api/tracks"),
            (HttpMethod.Post, $"/api/tracks/{id}/distribute"),
            (HttpMethod.Patch, $"/api/tracks/{id}/status")
        })
        {
            using var request = new HttpRequestMessage(method, path);
            using var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task DemoTokenEndpointIsNotRegisteredInProduction()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        using var response = await client.PostAsync("/api/auth/demo-token", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerIsPublicInDevelopmentAndDescribesProtectedOperations()
    {
        using var ui = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/openapi/v1.json"));
        var root = document.RootElement;
        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());
        var paths = root.GetProperty("paths");
        foreach (var (path, method) in new[]
        {
            ("/api/tracks", "get"), ("/api/tracks", "post"), ("/api/tracks/{id}", "get"),
            ("/api/tracks/{id}/distribute", "post"), ("/api/tracks/{id}/status", "patch")
        })
        {
            var security = paths.GetProperty(path).GetProperty(method).GetProperty("security");
            Assert.True(security[0].TryGetProperty("Bearer", out _));
        }
    }

    [Fact]
    public async Task AuthenticatedTrackLifecyclePreservesAssignmentsAndCombinesFilters()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var id = await CreateTrackAsync(_artist, " usabc2600001 ", "Jazz");
        var otherArtistTrack = await CreateTrackAsync(_otherArtist, "USABC2600002", "Jazz");
        var otherGenreTrack = await CreateTrackAsync(_artist, "USABC2600003", "Rock");
        await CreateTrackAsync(_artist, "USABC2600004", "Jazz");

        using var duplicate = await _client.PostAsJsonAsync("/api/tracks", TrackInput(_artist, "USABC2600001", "Jazz"));
        await AssertProblemAsync(duplicate, HttpStatusCode.Conflict);
        using var invalidTransition = await _client.PatchAsJsonAsync($"/api/tracks/{id}/status", new { status = "distributed" });
        await AssertProblemAsync(invalidTransition, HttpStatusCode.Conflict);
        using var invalidSelection = await _client.PostAsJsonAsync($"/api/tracks/{id}/distribute", new { dspIds = new[] { _dsp, int.MaxValue } });
        await AssertProblemAsync(invalidSelection, HttpStatusCode.NotFound);
        var unchanged = await _client.GetFromJsonAsync<JsonElement>($"/api/tracks/{id}");
        Assert.Equal("draft", unchanged.GetProperty("status").GetString());
        Assert.Empty(unchanged.GetProperty("assignments").EnumerateArray());

        using var submitted = await _client.PostAsJsonAsync($"/api/tracks/{id}/distribute", new { dspIds = new[] { _dsp, _dsp } });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var submittedBody = await submitted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("submitted", submittedBody.GetProperty("status").GetString());
        var assignment = Assert.Single(submittedBody.GetProperty("assignments").EnumerateArray());
        Assert.Equal("Test DSP", assignment.GetProperty("dspName").GetString());
        Assert.Equal("pending", assignment.GetProperty("status").GetString());
        var submittedAt = assignment.GetProperty("submittedAt").GetDateTimeOffset();
        Assert.Equal(TimeSpan.Zero, submittedAt.Offset);

        using var replay = await _client.PostAsJsonAsync($"/api/tracks/{id}/distribute", new { dspIds = new[] { _dsp } });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(submittedAt, Assert.Single(replayBody.GetProperty("assignments").EnumerateArray())
            .GetProperty("submittedAt").GetDateTimeOffset());

        foreach (var otherTrack in new[] { otherArtistTrack, otherGenreTrack })
        {
            using var otherSubmission = await _client.PostAsJsonAsync($"/api/tracks/{otherTrack}/distribute",
                new { dspIds = new[] { _dsp } });
            Assert.Equal(HttpStatusCode.OK, otherSubmission.StatusCode);
        }

        var summary = Assert.Single(await _client.GetFromJsonAsync<JsonElement[]>(
            $"/api/tracks?artistId={_artist}&genre=aZ&status=submitted") ?? []);
        Assert.Equal(id, summary.GetProperty("id").GetInt32());
        Assert.Equal("Test Artist", summary.GetProperty("artistName").GetString());

        using var update = await _client.PatchAsJsonAsync($"/api/tracks/{id}/status", new { status = "distributed" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("distributed", updated.GetProperty("status").GetString());
        using var sameStatus = await _client.PatchAsJsonAsync($"/api/tracks/{id}/status", new { status = "distributed" });
        Assert.Equal(HttpStatusCode.OK, sameStatus.StatusCode);
        var detail = await _client.GetFromJsonAsync<JsonElement>($"/api/tracks/{id}");
        Assert.Equal("USABC2600001", detail.GetProperty("isrc").GetString());
        Assert.Equal("2026-05-15", detail.GetProperty("releaseDate").GetString());
        Assert.Equal("pending", Assert.Single(detail.GetProperty("assignments").EnumerateArray()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task AuthenticatedRequestsReceiveProblemDetailsForInvalidInputAndMissingTracks()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        using var invalidStatus = await _client.PatchAsJsonAsync("/api/tracks/1/status", new { status = 2 });
        await AssertProblemAsync(invalidStatus, HttpStatusCode.BadRequest);
        using var missing = await _client.GetAsync("/api/tracks/1");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
        using var empty = await _client.PostAsJsonAsync("/api/tracks/1/distribute", new { dspIds = Array.Empty<int>() });
        await AssertProblemAsync(empty, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConcurrentDuplicateSubmissionReplaysTheCommittedAssignment()
    {
        _client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        var id = await CreateTrackAsync(_artist, "USABC2600001", "Jazz");
        await using var firstScope = _factory.Services.CreateAsyncScope();
        await using var secondScope = _factory.Services.CreateAsyncScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ISender>();
        var services = secondScope.ServiceProvider;
        var commit = new InterleavedCommit(services.GetRequiredService<IUnitOfWork>(), async cancellationToken =>
        {
            var winner = await first.Send(new DistributeTrackCommand(id, [_dsp]), cancellationToken);
            Assert.Null(winner.Error);
        });
        var second = ActivatorUtilities.CreateInstance<DistributeTrackHandler>(services, commit);

        var result = await second.Handle(new DistributeTrackCommand(id, [_dsp]), default);

        Assert.Null(result.Error);
        Assert.Single(result.Value!.Assignments);
        Assert.Equal(1, await services.GetRequiredService<TrackManagementDbContext>().TrackDistributions.CountAsync());
    }

    private async Task<int> CreateTrackAsync(int artistId, string isrc, string genre)
    {
        using var response = await _client.PostAsJsonAsync("/api/tracks", TrackInput(artistId, isrc, genre));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetInt32();
        Assert.Equal("draft", body.GetProperty("status").GetString());
        Assert.Equal($"/api/tracks/{id}", response.Headers.Location?.AbsolutePath);
        return id;
    }

    private static object TrackInput(int artistId, string isrc, string genre) =>
        new { title = "Test Track", artistId, isrc, releaseDate = "2026-05-15", genre };

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
    }

    private string CreateToken(string? invalidField = null)
    {
        var now = DateTime.UtcNow;
        var key = invalidField == "signature" ? RandomNumberGenerator.GetBytes(32) : _key;
        var token = new JwtSecurityToken(invalidField == "issuer" ? "wrong-issuer" : Issuer,
            invalidField == "audience" ? "wrong-audience" : Audience,
            notBefore: now.AddMinutes(-5), expires: now.AddMinutes(invalidField == "expiration" ? -1 : 5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _anchor.DisposeAsync();
    }

    private sealed class InterleavedCommit(IUnitOfWork inner, Func<CancellationToken, Task> competingCommit) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await competingCommit(cancellationToken);
            return await inner.SaveChangesAsync(cancellationToken);
        }
    }
}
