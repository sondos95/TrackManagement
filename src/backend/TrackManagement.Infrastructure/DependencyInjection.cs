using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackManagement.Application.Interfaces;
using TrackManagement.Infrastructure.Persistence;
using TrackManagement.Infrastructure.Repository;

namespace TrackManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString = null)
    {
        services.AddDbContext<TrackManagementDbContext>((provider, options) =>
        {
            var configuredConnection = connectionString ??
                provider.GetRequiredService<IConfiguration>().GetConnectionString("TrackManagement")
                ?? throw new InvalidOperationException("Connection string 'TrackManagement' is required.");
            options.UseSqlite(configuredConnection);
        });
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<ITrackRepository, TrackRepository>();
        services.AddScoped<ITrackQueries, TrackQueries>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
