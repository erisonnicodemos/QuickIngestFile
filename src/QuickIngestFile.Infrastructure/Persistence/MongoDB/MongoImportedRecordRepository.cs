namespace QuickIngestFile.Infrastructure.Persistence.MongoDB;

using global::MongoDB.Driver;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// MongoDB implementation of ImportedRecord repository with bulk insert support.
/// </summary>
public sealed class MongoImportedRecordRepository : IImportedRecordRepository
{
    private readonly IMongoCollection<ImportedRecord> _collection;

    public MongoImportedRecordRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ImportedRecord>("importedRecords");

        // Create indexes
        var indexKeys = Builders<ImportedRecord>.IndexKeys
            .Ascending(x => x.ImportJobId)
            .Ascending(x => x.RowNumber);

        _collection.Indexes.CreateOne(new CreateIndexModel<ImportedRecord>(indexKeys));
    }

    public async Task<ImportedRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(_ => true)
            .SortBy(x => x.RowNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportedRecord>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

        var items = await _collection.Find(_ => true)
            .SortBy(x => x.RowNumber)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportedRecord>(items, (int)totalCount, page, pageSize);
    }

    public async Task<ImportedRecord> AddAsync(ImportedRecord entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<ImportedRecord> entities, CancellationToken cancellationToken = default)
    {
        var list = entities.ToList();
        if (list.Count > 0)
        {
            await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
        }
    }

    public async Task UpdateAsync(ImportedRecord entity, CancellationToken cancellationToken = default)
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

    public async Task<IReadOnlyList<ImportedRecord>> GetByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(x => x.ImportJobId == importJobId)
            .SortBy(x => x.RowNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportedRecord>> GetPagedByImportJobIdAsync(Guid importJobId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ImportedRecord>.Filter.Eq(x => x.ImportJobId, importJobId);

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await _collection.Find(filter)
            .SortBy(x => x.RowNumber)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportedRecord>(items, (int)totalCount, page, pageSize);
    }

    public async Task BulkInsertAsync(IEnumerable<ImportedRecord> records, CancellationToken cancellationToken = default)
    {
        var list = records.ToList();
        if (list.Count > 0)
        {
            await _collection.InsertManyAsync(list, new InsertManyOptions { IsOrdered = false }, cancellationToken);
        }
    }

    public async Task DeleteByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        await _collection.DeleteManyAsync(x => x.ImportJobId == importJobId, cancellationToken);
    }

    public async Task<int> CountByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return (int)await _collection.CountDocumentsAsync(x => x.ImportJobId == importJobId, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedRecord>> SearchAsync(Guid importJobId, string searchTerm, CancellationToken cancellationToken = default)
    {
        // Text search in DataJson
        var filter = Builders<ImportedRecord>.Filter.And(
            Builders<ImportedRecord>.Filter.Eq(x => x.ImportJobId, importJobId),
            Builders<ImportedRecord>.Filter.Regex(x => x.DataJson, new global::MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
        );

        return await _collection.Find(filter)
            .SortBy(x => x.RowNumber)
            .Limit(100)
            .ToListAsync(cancellationToken);
    }
}
