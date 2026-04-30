using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Home;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Avalonia.Services;
using CVAnalysisHub.Avalonia.ViewModels;
using CVAnalysisHub.Avalonia.Views;
using CVAnalysisHub.Infrastructure;
using CVAnalysisHub.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CVAnalysisHub.Avalonia;

public partial class App : global::Avalonia.Application
{
    private IHost? host;
    private Task? hostStartTask;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddInfrastructure(context.Configuration);
                services.AddTransient<HomeViewModel>();
                services.AddTransient<VideoListViewModel>();
                services.AddTransient<AnalysisRunListViewModel>();
                services.AddTransient<CreateVideoViewModel>();
                services.AddTransient<CreateAnalysisRunViewModel>();
                services.AddTransient<AnalysisRunDetailsViewModel>();
                services.AddTransient<DesktopMediaService>();
                services.AddTransient<DesktopNotificationViewModel>();
                services.AddTransient<DesktopUploadViewModel>();
                services.AddTransient<VideoWorkspaceViewModel>();
                services.AddTransient<AnalysisWorkspaceViewModel>();
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        EnsureConfiguredStorage(host.Services.GetRequiredService<IConfiguration>());

        Task.Run(async () =>
        {
            using var scope = host.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbInitializer>()
                .InitializeAsync();
        }).GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;
            desktop.MainWindow = host.Services.GetRequiredService<MainWindow>();
        }

        hostStartTask = Task.Run(() => host.StartAsync());

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (host is null)
        {
            return;
        }

        if (hostStartTask is not null)
        {
            try
            {
                await hostStartTask;
            }
            catch
            {
            }
        }

        await host.StopAsync();
        host.Dispose();
        host = null;
        hostStartTask = null;
    }

    private static void EnsureConfiguredStorage(IConfiguration configuration)
    {
        var mediaRoot = configuration["MediaStorage:RootPath"];
        var modelPath = configuration["ComputerVision:YoloDotNet:ModelPath"];
        var sqliteConnectionString = configuration.GetConnectionString("Sqlite");

        if (!string.IsNullOrWhiteSpace(mediaRoot))
        {
            Directory.CreateDirectory(Path.GetFullPath(mediaRoot));
        }

        if (!string.IsNullOrWhiteSpace(modelPath))
        {
            var modelDirectory = Path.GetDirectoryName(Path.GetFullPath(modelPath));

            if (!string.IsNullOrWhiteSpace(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
            }
        }

        var sqliteDataSource = ResolveSqliteDataSource(sqliteConnectionString);

        if (!string.IsNullOrWhiteSpace(sqliteDataSource))
        {
            var sqliteDirectory = Path.GetDirectoryName(Path.GetFullPath(sqliteDataSource));

            if (!string.IsNullOrWhiteSpace(sqliteDirectory))
            {
                Directory.CreateDirectory(sqliteDirectory);
            }
        }
    }

    private static string? ResolveSqliteDataSource(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);

            if (parts.Length == 2 &&
                string.Equals(parts[0], "Data Source", StringComparison.OrdinalIgnoreCase))
            {
                return parts[1];
            }
        }

        return null;
    }
}
