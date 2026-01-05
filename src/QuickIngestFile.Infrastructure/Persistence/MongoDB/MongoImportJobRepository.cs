namespace QuickIngestFile.Infrastructure.Persistence.MongoDB;

using global::MongoDB.Driver;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// MongoDB implementation of ImportJob repository.
/// </summary>
public sealed class MongoImportJobRepository : IImportJobRepository
{
    private readonly IMongoCollection<ImportJob> _collection;

    public MongoImportJobRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ImportJob>("importJobs");
    }

    public async Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportJob>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

        var items = await _collection.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportJob>(items, (int)totalCount, page, pageSize);
    }

    public async Task<ImportJob> AddAsync(ImportJob entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<ImportJob> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        if (list.Count > 0)
        {
            await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        }
    }

    public async Task UpdateAsync(ImportJob entity, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return (int)await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ImportJob>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Limit(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportJob?> GetLatestByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.FileName == fileName)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
