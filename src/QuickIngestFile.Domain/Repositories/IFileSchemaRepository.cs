namespace QuickIngestFile.Domain.Repositories;

using QuickIngestFile.Domain.Entities;

/// <summary>
/// Repository for file schema definitions.
/// </summary>
public interface IFileSchemaRepository : IRepository<FileSchema>
{
    /// <summary>
    /// Get schema for a specific import job.
    /// </summary>
    Task<FileSchema?> GetByImportJobIdAsync(
        Guid importJobId, 
        CancellationToken cancellationToken = default);
}
