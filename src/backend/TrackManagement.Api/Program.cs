using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using TrackManagement.Application.Features.Artists.Commands;
using TrackManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters.RequireSignedTokens = true;
    options.TokenValidationParameters.RequireExpirationTime = true;
    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidateAudience = true;
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<CreateArtistCommand>());

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, _) =>
        {
            if (operation.Parameters is not null)
            {
                foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
                {
                    if (parameter.In == ParameterLocation.Path && parameter.Name == "id")
                    {
                        parameter.Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
                    }
                }
            }

            if (!context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document)] = []
                });
            }

            return Task.CompletedTask;
        });
    });
}

builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "An unexpected error occurred."
    };

    if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext
        { HttpContext = context, ProblemDetails = problem }))
    {
        await context.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Track Management API"));
    app.MapOpenApi().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
