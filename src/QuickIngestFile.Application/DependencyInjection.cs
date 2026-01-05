namespace QuickIngestFile.Application;

using Microsoft.Extensions.DependencyInjection;
using QuickIngestFile.Application.Parsing;
using QuickIngestFile.Application.Services;

/// <summary>
/// Dependency injection configuration for Application layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register parsers
        services.AddSingleton<IFileParser, CsvFileParser>();
        services.AddSingleton<IFileParser, ExcelFileParser>();
        services.AddSingleton<FileParserFactory>();

        // Register services
        services.AddScoped<ImportService>();
        services.AddScoped<ImportJobService>();
        services.AddScoped<DataQueryService>();
        
        // Register background import queue (singleton for shared state)
        services.AddSingleton<BackgroundImportQueue>();
        
        // Register background worker for parallel import processing
        services.AddHostedService<ImportBackgroundWorker>();

        return services;
    }
}
