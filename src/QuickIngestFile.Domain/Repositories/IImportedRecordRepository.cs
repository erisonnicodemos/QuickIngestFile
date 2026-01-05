namespace QuickIngestFile.Domain.Repositories;

using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;

/// <summary>
/// Repository for imported records with dynamic data.
/// </summary>
public interface IImportedRecordRepository : IRepository<ImportedRecord>
{
    /// <summary>
    /// Get all records for a specific import job.
    /// </summary>
    Task<IReadOnlyList<ImportedRecord>> GetByImportJobIdAsync(
        Guid importJobId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated records for a specific import job.
    /// </summary>
    Task<PagedResult<ImportedRecord>> GetPagedByImportJobIdAsync(
        Guid importJobId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk insert records for high-performance import.
    /// </summary>
    Task BulkInsertAsync(
        IEnumerable<ImportedRecord> records, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all records for an import job.
    /// </summary>
    Task DeleteByImportJobIdAsync(
        Guid importJobId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count records for an import job.
    /// </summary>
    Task<int> CountByImportJobIdAsync(
        Guid importJobId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search records by value in any column.
    /// </summary>
    Task<IReadOnlyList<ImportedRecord>> SearchAsync(
        Guid importJobId,
        string searchTerm,
        CancellationToken cancellationToken = default);
}
