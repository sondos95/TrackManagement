using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TrackManagement.Api.Auth;

internal sealed class DemoTokens
{
    internal const string ReadPolicy = "TrackRead";
    internal const string PermissionClaim = "track_management_permission";
    internal const string ReadOnly = "demo_read";
    private readonly SymmetricSecurityKey _key = new(RandomNumberGenerator.GetBytes(32))
    {
        KeyId = Guid.NewGuid().ToString("N")
    };

    internal void AddValidationKey(JwtBearerOptions options)
    {
        var parameters = options.TokenValidationParameters;
        parameters.IssuerSigningKeys = (parameters.IssuerSigningKeys ?? []).Append(_key).ToArray();
    }

    internal IResult Issue(HttpContext context, IOptionsMonitor<JwtBearerOptions> options, TimeProvider clock)
    {
        var validation = options.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        var issuer = validation.ValidIssuer ?? validation.ValidIssuers?.FirstOrDefault();
        var audience = validation.ValidAudience ?? validation.ValidAudiences?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            return Results.Problem("Development JWT issuer and audience must be configured.");

        var now = clock.GetUtcNow().UtcDateTime;
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, "takwene-demo"), new Claim(PermissionClaim, ReadOnly)],
            notBefore: now, expires: now.AddMinutes(15),
            signingCredentials: new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));

        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}
