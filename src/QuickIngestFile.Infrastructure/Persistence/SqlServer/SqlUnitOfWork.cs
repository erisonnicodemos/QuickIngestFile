namespace QuickIngestFile.Infrastructure.Persistence.SqlServer;

using Microsoft.EntityFrameworkCore.Storage;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// SQL Server implementation of Unit of Work.
/// </summary>
public sealed class SqlUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public SqlUnitOfWork(AppDbContext context)
    {
        _context = context;
        ImportJobs = new SqlImportJobRepository(context);
        ImportedRecords = new SqlImportedRecordRepository(context);
        FileSchemas = new SqlFileSchemaRepository(context);
    }

    public IImportJobRepository ImportJobs { get; }
    public IImportedRecordRepository ImportedRecords { get; }
    public IFileSchemaRepository FileSchemas { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
