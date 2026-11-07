using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TrackManagement.Application.Features.Artists.DTOs.Responses;
using TrackManagement.Infrastructure.Persistence;
using Xunit;

namespace TrackManagement.IntegrationTests.Artists;

public sealed class ArtistApiTests
{
    [Fact]
    public async Task ArtistRoutesRequireJwtAndPersistOnlyValidArtists()
    {
        const string route = "/api/artists";
        const string issuer = "artist-integration";
        const string audience = "artist-api";
        var key = RandomNumberGenerator.GetBytes(32);
        var connectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;Foreign Keys=True;Pooling=False";
        await using var anchor = new SqliteConnection(connectionString);
        await anchor.OpenAsync();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TrackManagement"] = connectionString,
                    ["Authentication:Schemes:Bearer:ValidIssuer"] = issuer,
                    ["Authentication:Schemes:Bearer:ValidAudiences:0"] = audience,
                    ["Authentication:Schemes:Bearer:SigningKeys:0:Issuer"] = issuer,
                    ["Authentication:Schemes:Bearer:SigningKeys:0:Value"] = Convert.ToBase64String(key)
                }));
        });
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrackManagementDbContext>();
        await db.Database.MigrateAsync();

        using var anonymousGet = await client.GetAsync(route);
        using var anonymousPost = await client.PostAsJsonAsync(route,
            new { name = "Artist", email = "artist@example.com", country = "EG" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousGet.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPost.StatusCode);

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var artistPath = document.RootElement.GetProperty("paths").GetProperty(route);
        Assert.True(artistPath.GetProperty("get").GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.True(artistPath.GetProperty("post").GetProperty("security")[0].TryGetProperty("Bearer", out _));

        var token = new JwtSecurityToken(issuer, audience, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(token));

        using var invalid = await client.PostAsJsonAsync(route,
            new { name = " ", email = "not-an-email", country = new string('X', 101) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        var errors = (await invalid.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _));
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("country", out _));
        Assert.Equal(0, await db.Artists.CountAsync());
        Assert.Empty(Assert.IsType<ArtistDto[]>(await client.GetFromJsonAsync<ArtistDto[]>(route)));

        var zulu = await CreateArtistAsync(client, " Zulu ", " zulu@example.com ");
        var firstAlpha = await CreateArtistAsync(client, "Alpha", "alpha@example.com");
        var secondAlpha = await CreateArtistAsync(client, "Alpha", "another@example.com");

        Assert.True(zulu.Id > 0);
        Assert.Equal(new ArtistDto(zulu.Id, "Zulu", "zulu@example.com", "EG"), zulu);
        Assert.Equal(new[] { firstAlpha, secondAlpha, zulu }, await client.GetFromJsonAsync<ArtistDto[]>(route));
    }

    private static async Task<ArtistDto> CreateArtistAsync(HttpClient client, string name, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/artists", new { name, email, country = " EG " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ArtistDto>(await response.Content.ReadFromJsonAsync<ArtistDto>());
    }
}
