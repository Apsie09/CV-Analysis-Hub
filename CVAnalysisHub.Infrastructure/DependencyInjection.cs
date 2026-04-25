using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Infrastructure.AnalysisRuns;
using CVAnalysisHub.Infrastructure.ComputerVision;
using CVAnalysisHub.Infrastructure.Media;
using CVAnalysisHub.Infrastructure.Persistence;
using CVAnalysisHub.Infrastructure.Videos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CVAnalysisHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var persistenceOptions = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();
        var provider = persistenceOptions.Provider.Trim();
        var connectionStringName = ResolveConnectionStringName(provider);
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is not configured for provider '{provider}'.");
        }

        services.Configure<ComputerVisionOptions>(configuration.GetSection("ComputerVision"));
        services.Configure<MediaStorageOptions>(configuration.GetSection("MediaStorage"));
        services.Configure<VideoProcessingOptions>(configuration.GetSection("VideoProcessing"));
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.AddDbContextFactory<AppDbContext>(options => ConfigureDbContext(options, provider, connectionString));
        services.AddScoped<AppDbInitializer>();
        services.AddSingleton<MediaStorageService>();
        services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<VideoProcessingOptions>>().Value);
        services.AddSingleton<VideoProcessingService>();
        services.AddScoped<IAnalysisRunProcessor, EfCoreAnalysisRunProcessor>();
        services.AddScoped<IAnalysisRunService, EfCoreAnalysisRunService>();
        services.AddSingleton<PlaceholderAnalysisInferenceEngine>();
        services.AddSingleton<YoloDotNetAnalysisInferenceEngine>();
        services.AddSingleton<IAnalysisInferenceEngine, ConfigurableAnalysisInferenceEngine>();
        services.AddScoped<IVideoService, EfCoreVideoService>();
        services.AddHostedService<QueuedAnalysisBackgroundService>();

        return services;
    }

    private static void ConfigureDbContext(
        DbContextOptionsBuilder options,
        string provider,
        string connectionString)
    {
        switch (provider.Trim().ToLowerInvariant())
        {
            case "postgres":
            case "postgresql":
            case "npgsql":
                options.UseNpgsql(connectionString);
                break;
            case "sqlite":
                options.UseSqlite(connectionString);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported database provider '{provider}'. Supported values are 'Sqlite' and 'PostgreSql'.");
        }
    }

    private static string ResolveConnectionStringName(string provider)
    {
        return provider.Trim().ToLowerInvariant() switch
        {
            "postgres" or "postgresql" or "npgsql" => "PostgreSql",
            "sqlite" => "Sqlite",
            _ => throw new InvalidOperationException(
                $"Unsupported database provider '{provider}'. Supported values are 'Sqlite' and 'PostgreSql'.")
        };
    }
}
