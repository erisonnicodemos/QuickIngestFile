namespace QuickIngestFile.Infrastructure.Persistence.SqlServer;

using Microsoft.EntityFrameworkCore;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// SQL Server implementation of FileSchema repository.
/// </summary>
public sealed class SqlFileSchemaRepository(AppDbContext context) : IFileSchemaRepository
{
    public async Task<FileSchema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.FileSchemas.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<FileSchema>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.FileSchemas.ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<FileSchema>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await context.FileSchemas.CountAsync(cancellationToken);

        var items = await context.FileSchemas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FileSchema>(items, totalCount, page, pageSize);
    }

    public async Task<FileSchema> AddAsync(FileSchema entity, CancellationToken cancellationToken = default)
    {
        await context.FileSchemas.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<FileSchema> entities, CancellationToken cancellationToken = default)
    {
        await context.FileSchemas.AddRangeAsync(entities, cancellationToken);
    }

    public Task UpdateAsync(FileSchema entity, CancellationToken cancellationToken = default)
    {
        context.FileSchemas.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.FileSchemas.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            context.FileSchemas.Remove(entity);
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await context.FileSchemas.CountAsync(cancellationToken);
    }

    public async Task<FileSchema?> GetByImportJobIdAsync(Guid importJobId, CancellationToken cancellationToken = default)
    {
        return await context.FileSchemas
            .FirstOrDefaultAsync(s => s.ImportJobId == importJobId, cancellationToken);
    }
}
