namespace QuickIngestFile.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QuickIngestFile.Domain.Repositories;
using QuickIngestFile.Infrastructure.Configuration;
using QuickIngestFile.Infrastructure.Persistence.MongoDB;
using QuickIngestFile.Infrastructure.Persistence.SqlServer;

/// <summary>
/// Dependency injection configuration for Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DatabaseOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register database provider based on configuration
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            return options.Provider switch
            {
                DatabaseProvider.MongoDB => CreateMongoUnitOfWork(sp, options),
                _ => CreateSqlUnitOfWork(sp)
            };
        });

        return services;
    }

    public static IServiceCollection AddSqlServerDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(60);
            });
        });

        return services;
    }

    public static IServiceCollection AddMongoDatabase(
        this IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        return services;
    }

    private static IUnitOfWork CreateSqlUnitOfWork(IServiceProvider sp)
    {
        var context = sp.GetRequiredService<AppDbContext>();
        return new SqlUnitOfWork(context);
    }

    private static IUnitOfWork CreateMongoUnitOfWork(IServiceProvider sp, DatabaseOptions options)
    {
        var client = new MongoClient(options.MongoDB.ConnectionString);
        var database = client.GetDatabase(options.MongoDB.DatabaseName);
        return new MongoUnitOfWork(client, database);
    }
}
