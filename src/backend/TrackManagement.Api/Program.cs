using Microsoft.AspNetCore.Mvc;
using TrackManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var connectionString = builder.Configuration.GetConnectionString("TrackManagement")
    ?? throw new InvalidOperationException("Connection string 'TrackManagement' is required.");
builder.Services.AddInfrastructure(connectionString);

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
    app.MapOpenApi();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
