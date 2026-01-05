namespace QuickIngestFile.Application.Services;

using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// Service for querying imported data dynamically.
/// </summary>
public sealed class DataQueryService(IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Get schema for an import job.
    /// </summary>
    public async Task<Result<FileSchemaDto>> GetSchemaAsync(
        Guid importJobId,
        CancellationToken cancellationToken = default)
    {
        var schema = await unitOfWork.FileSchemas.GetByImportJobIdAsync(importJobId, cancellationToken);

        if (schema is null)
            return Result.Failure<FileSchemaDto>($"Schema not found for import job {importJobId}");

        var columns = schema.GetColumns().Select(c => new ColumnDefinitionDto(
            c.Name,
            c.Index,
            c.DetectedType,
            c.DisplayName,
            c.IsIgnored)).ToList();

        return Result.Success(new FileSchemaDto(
            schema.Id,
            schema.ImportJobId,
            schema.FileName,
            columns));
    }

    /// <summary>
    /// Get paginated records for an import job.
    /// </summary>
    public async Task<PagedResult<ImportedRecordDto>> GetRecordsAsync(
        Guid importJobId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await unitOfWork.ImportedRecords.GetPagedByImportJobIdAsync(
            importJobId, page, pageSize, cancellationToken);

        var dtos = result.Items.Select(r => new ImportedRecordDto(
            r.Id,
            r.ImportJobId,
            r.RowNumber,
            r.GetData())).ToList();

        return new PagedResult<ImportedRecordDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }

    /// <summary>
    /// Search records by value.
    /// </summary>
    public async Task<IReadOnlyList<ImportedRecordDto>> SearchRecordsAsync(
        Guid importJobId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var records = await unitOfWork.ImportedRecords.SearchAsync(
            importJobId, searchTerm, cancellationToken);

        return records.Select(r => new ImportedRecordDto(
            r.Id,
            r.ImportJobId,
            r.RowNumber,
            r.GetData())).ToList();
    }

    /// <summary>
    /// Get record count for an import job.
    /// </summary>
    public async Task<int> GetRecordCountAsync(
        Guid importJobId,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ImportedRecords.CountByImportJobIdAsync(importJobId, cancellationToken);
    }

    /// <summary>
    /// Delete all records for an import job.
    /// </summary>
    public async Task DeleteImportDataAsync(
        Guid importJobId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.ImportedRecords.DeleteByImportJobIdAsync(importJobId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
