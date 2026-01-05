namespace QuickIngestFile.Domain.Repositories;

using QuickIngestFile.Domain.Entities;

/// <summary>
/// Import job repository interface.
/// </summary>
public interface IImportJobRepository : IRepository<ImportJob>
{
    Task<IReadOnlyList<ImportJob>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<ImportJob?> GetLatestByFileNameAsync(string fileName, CancellationToken cancellationToken = default);
}
