namespace QuickIngestFile.Infrastructure.Persistence.SqlServer;

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// SQL Server implementation of ImportedRecord repository with bulk insert support.
/// </summary>
public sealed class SqlImportedRecordRepository(AppDbContext context) : IImportedRecordRepository
{
    public async Task<ImportedRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ImportedRecords.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.ImportedRecords
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportedRecord>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await context.ImportedRecords.CountAsync(cancellationToken);

        var items = await context.ImportedRecords
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportedRecord>(items, totalCount, page, pageSize);
    }

    public async Task<ImportedRecord> AddAsync(ImportedRecord entity, CancellationToken cancellationToken = default)
    {
        await context.ImportedRecords.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<ImportedRecord> entities, CancellationToken cancellationToken = default)
    {
        await context.ImportedRecords.AddRangeAsync(entities, cancellationToken);
    }

    public Task UpdateAsync(ImportedRecord entity, CancellationToken cancellationToken = default)
    {
        context.ImportedRecords.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.ImportedRecords.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            context.ImportedRecords.Remove(entity);
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await context.ImportedRecords.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedRecord>> GetByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedRecords
            .Where(r => r.ImportJobId == importJobId)
            .OrderBy(r => r.RowNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportedRecord>> GetPagedByImportJobIdAsync(Guid importJobId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.ImportedRecords.Where(r => r.ImportJobId == importJobId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportedRecord>(items, totalCount, page, pageSize);
    }

    public async Task BulkInsertAsync(IEnumerable<ImportedRecord> records, CancellationToken cancellationToken = default)
    {
        var recordList = records.ToList();
        if (recordList.Count == 0)
            return;

        await context.BulkInsertAsync(recordList, cancellationToken: cancellationToken);
    }

    public async Task DeleteByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        await context.ImportedRecords
            .Where(r => r.ImportJobId == importJobId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await context.ImportedRecords
            .Where(r => r.ImportJobId == importJobId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedRecord>> SearchAsync(Guid importJobId, string searchTerm, CancellationToken cancellationToken = default)
    {
        // Search in JSON column - works with SQL Server and PostgreSQL
        return await context.ImportedRecords
            .Where(r => r.ImportJobId == importJobId && r.DataJson.Contains(searchTerm))
            .OrderBy(r => r.RowNumber)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}
