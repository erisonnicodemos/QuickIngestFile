namespace QuickIngestFile.Application.DTOs;

using QuickIngestFile.Domain.Entities;

/// <summary>
/// DTO for import job information.
/// </summary>
public sealed record ImportJobDto(
    Guid Id,
    string FileName,
    string FileType,
    long FileSize,
    int TotalRecords,
    int ProcessedRecords,
    int FailedRecords,
    string Status,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? DurationMs,
    string? ErrorMessage)
{
    public static ImportJobDto FromEntity(ImportJob job) => new(
        job.Id,
        job.FileName,
        job.FileType,
        job.FileSize,
        job.TotalRecords,
        job.ProcessedRecords,
        job.FailedRecords,
        job.Status.ToString(),
        job.CreatedAt,
        job.StartedAt,
        job.CompletedAt,
        job.Duration?.TotalMilliseconds,
        job.ErrorMessage);
}

/// <summary>
/// Progress update for real-time tracking.
/// </summary>
public sealed record ImportProgressDto(
    Guid JobId,
    int ProcessedRecords,
    int TotalRecords,
    double ProgressPercentage,
    string Status);

/// <summary>
/// DTO for imported record data.
/// </summary>
public sealed record ImportedRecordDto(
    Guid Id,
    Guid ImportJobId,
    int RowNumber,
    Dictionary<string, object?> Data);

/// <summary>
/// DTO for file schema/structure.
/// </summary>
public sealed record FileSchemaDto(
    Guid Id,
    Guid ImportJobId,
    string FileName,
    IReadOnlyList<ColumnDefinitionDto> Columns);

/// <summary>
/// DTO for column definition.
/// </summary>
public sealed record ColumnDefinitionDto(
    string Name,
    int Index,
    string DetectedType,
    string? DisplayName = null,
    bool IsIgnored = false);

/// <summary>
/// Request to start an import with optional configuration.
/// </summary>
public sealed record StartImportRequest(
    char Delimiter = ';',
    bool HasHeader = false,
    int SkipRows = 0,
    string? SheetName = null);

/// <summary>
/// Preview data from a file before full import.
/// </summary>
public sealed record FilePreviewDto(
    string FileName,
    long FileSize,
    IReadOnlyList<ColumnDefinitionDto> DetectedColumns,
    IReadOnlyList<Dictionary<string, object?>> PreviewRows,
    int EstimatedTotalRows);
