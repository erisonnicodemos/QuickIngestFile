namespace QuickIngestFile.Domain.Repositories;

/// <summary>
/// Unit of Work interface for transaction management.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IImportJobRepository ImportJobs { get; }
    IImportedRecordRepository ImportedRecords { get; }
    IFileSchemaRepository FileSchemas { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
