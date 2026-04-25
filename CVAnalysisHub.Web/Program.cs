using CVAnalysisHub.Web.Components;
using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Application.Home;
using CVAnalysisHub.Application.Videos;
using CVAnalysisHub.Infrastructure;
using CVAnalysisHub.Infrastructure.Media;
using CVAnalysisHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
EnsureConfiguredStorage(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddTransient<AnalysisRunDetailsViewModel>();
builder.Services.AddTransient<AnalysisRunListViewModel>();
builder.Services.AddTransient<CreateAnalysisRunViewModel>();
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<VideoListViewModel>();
builder.Services.AddTransient<CreateVideoViewModel>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<AppDbInitializer>();
    await dbInitializer.InitializeAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

var contentTypeProvider = new FileExtensionContentTypeProvider();
app.MapGet("/media/{**relativePath}", ServeMediaFile);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void EnsureConfiguredStorage(IConfiguration configuration)
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

static string? ResolveSqliteDataSource(string? connectionString)
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

IResult ServeMediaFile(string relativePath, MediaStorageService mediaStorageService)
{
    if (!mediaStorageService.TryGetPhysicalPath(relativePath, out var physicalPath) || !File.Exists(physicalPath))
    {
        return Results.NotFound();
    }

    var contentType = contentTypeProvider.TryGetContentType(physicalPath, out var resolvedContentType)
        ? resolvedContentType
        : "application/octet-stream";

    return Results.File(physicalPath, contentType, enableRangeProcessing: true);
}
