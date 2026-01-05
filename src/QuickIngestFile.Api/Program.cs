using QuickIngestFile.Application;
using QuickIngestFile.Infrastructure;
using QuickIngestFile.Infrastructure.Configuration;
using QuickIngestFile.Infrastructure.Persistence.SqlServer;
using QuickIngestFile.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "QuickIngestFile API", 
        Version = "v1",
        Description = "High-performance file ingestion API supporting multiple file formats and databases"
    });
});

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Application layer
builder.Services.AddApplication();

// Configure database
var databaseProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "SQLite";

if (databaseProvider == DatabaseProvider.MongoDB)
{
    var mongoConnectionString = builder.Configuration.GetValue<string>("Database:MongoDB:ConnectionString") 
        ?? "mongodb://localhost:27017";
    var mongoDatabaseName = builder.Configuration.GetValue<string>("Database:MongoDB:DatabaseName") 
        ?? "quickingestfile";

    builder.Services.AddMongoDatabase(mongoConnectionString, mongoDatabaseName);
}
else if (databaseProvider == DatabaseProvider.SQLite)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=quickingestfile.db";

    builder.Services.AddSqliteDatabase(connectionString);
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=QuickIngestFile;Trusted_Connection=True;";

    builder.Services.AddSqlServerDatabase(connectionString);
}

// Add Infrastructure layer
builder.Services.AddInfrastructure(options =>
{
    options.Provider = databaseProvider;
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    options.MongoDB.ConnectionString = builder.Configuration.GetValue<string>("Database:MongoDB:ConnectionString") 
        ?? "mongodb://localhost:27017";
    options.MongoDB.DatabaseName = builder.Configuration.GetValue<string>("Database:MongoDB:DatabaseName") 
        ?? "quickingestfile";
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickIngestFile API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");

// Ensure database is created (for SQL/SQLite)
if (databaseProvider != DatabaseProvider.MongoDB)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Map endpoints
app.MapImportEndpoints();
app.MapDataEndpoints();
app.MapJobEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithTags("Health");

app.Run();
