namespace QuickIngestFile.Infrastructure.Persistence.SqlServer;

using Microsoft.EntityFrameworkCore;
using QuickIngestFile.Domain.Entities;

/// <summary>
/// Entity Framework Core DbContext for SQL Server/PostgreSQL.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportedRecord> ImportedRecords => Set<ImportedRecord>();
    public DbSet<FileSchema> FileSchemas => Set<FileSchema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ImportJob configuration
        modelBuilder.Entity<ImportJob>(entity =>
        {
            entity.ToTable("ImportJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Status);
        });

        // ImportedRecord configuration
        modelBuilder.Entity<ImportedRecord>(entity =>
        {
            entity.ToTable("ImportedRecords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataJson).IsRequired();
            entity.HasIndex(e => e.ImportJobId);
            entity.HasIndex(e => e.RowNumber);
        });

        // FileSchema configuration
        modelBuilder.Entity<FileSchema>(entity =>
        {
            entity.ToTable("FileSchemas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ColumnsJson).IsRequired();
            entity.HasIndex(e => e.ImportJobId).IsUnique();
        });
    }
}
