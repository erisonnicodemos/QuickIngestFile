namespace QuickIngestFile.Infrastructure.Configuration;

/// <summary>
/// Database configuration options.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database provider to use: SqlServer, PostgreSQL, or MongoDB
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Connection string for SQL databases.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// MongoDB-specific settings.
    /// </summary>
    public MongoDbSettings MongoDB { get; set; } = new();
}

/// <summary>
/// MongoDB-specific settings.
/// </summary>
public sealed class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "quickingestfile";
}

/// <summary>
/// Supported database providers.
/// </summary>
public static class DatabaseProvider
{
    public const string SqlServer = "SqlServer";
    public const string PostgreSQL = "PostgreSQL";
    public const string MongoDB = "MongoDB";
}
