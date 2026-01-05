namespace QuickIngestFile.Infrastructure.Persistence.SqlServer;

using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// SQL Server implementation of ImportJob repository.
/// </summary>
public sealed class SqlImportJobRepository(AppDbContext context) : IImportJobRepository
{
    public async Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ImportJobs.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<ImportJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.ImportJobs
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ImportJob>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await context.ImportJobs.CountAsync(cancellationToken);

        var items = await context.ImportJobs
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportJob>(items, totalCount, page, pageSize);
    }

    public async Task<ImportJob> AddAsync(ImportJob entity, CancellationToken cancellationToken = default)
    {
        await context.ImportJobs.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<ImportJob> entities, CancellationToken cancellationToken = default)
    {
        await context.ImportJobs.AddRangeAsync(entities, cancellationToken);
    }

    public Task UpdateAsync(ImportJob entity, CancellationToken cancellationToken = default)
    {
        context.ImportJobs.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.ImportJobs.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            context.ImportJobs.Remove(entity);
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await context.ImportJobs.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImportJob>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await context.ImportJobs
            .OrderByDescending(j => j.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportJob?> GetLatestByFileNameAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return await context.ImportJobs
            .Where(j => j.FileName == fileName)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
