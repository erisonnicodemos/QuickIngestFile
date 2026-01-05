namespace QuickIngestFile.Infrastructure.Persistence.MongoDB;

using global::MongoDB.Driver;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// MongoDB implementation of Unit of Work.
/// Note: MongoDB doesn't support traditional transactions across collections without replica sets.
/// This implementation provides a consistent interface but transactions are optional.
/// </summary>
public sealed class MongoUnitOfWork : IUnitOfWork
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private IClientSessionHandle? _session;

    public MongoUnitOfWork(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _database = database;
        ImportJobs = new MongoImportJobRepository(database);
        ImportedRecords = new MongoImportedRecordRepository(database);
        FileSchemas = new MongoFileSchemaRepository(database);
    }

    public IImportJobRepository ImportJobs { get; }
    public IImportedRecordRepository ImportedRecords { get; }
    public IFileSchemaRepository FileSchemas { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB writes are immediate, no SaveChanges needed
        return Task.FromResult(0);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
        _session.StartTransaction();
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
        {
            await _session.CommitTransactionAsync(cancellationToken);
            _session.Dispose();
            _session = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_session is not null)
        {
            await _session.AbortTransactionAsync(cancellationToken);
            _session.Dispose();
            _session = null;
        }
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
