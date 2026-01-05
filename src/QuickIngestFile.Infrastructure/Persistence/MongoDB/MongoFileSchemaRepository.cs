namespace QuickIngestFile.Infrastructure.Persistence.MongoDB;

using global::MongoDB.Driver;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// MongoDB implementation of FileSchema repository.
/// </summary>
public sealed class MongoFileSchemaRepository : IFileSchemaRepository
{
    private readonly IMongoCollection<FileSchema> _collection;

    public MongoFileSchemaRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<FileSchema>("fileSchemas");

        // Create unique index on ImportJobId
        var indexKeys = Builders<FileSchema>.IndexKeys.Ascending(x => x.ImportJobId);
        _collection.Indexes.CreateOne(new CreateIndexModel<FileSchema>(indexKeys, new CreateIndexOptions { Unique = true }));
    }

    public async Task<FileSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileSchema>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<FileSchema>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

        var items = await _collection.Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FileSchema>(items, (int)totalCount, page, pageSize);
    }

    public async Task<FileSchema> AddAsync(FileSchema entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<FileSchema> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        if (list.Count > 0)
        {
            await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        }
    }

    public async Task UpdateAsync(FileSchema entity, CancellationToken cancellationToken = default)
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

    public async Task<FileSchema?> GetByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.ImportJobId == importJobId).FirstOrDefaultAsync(cancellationToken);
    }
}
