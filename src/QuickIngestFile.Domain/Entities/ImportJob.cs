namespace QuickIngestFile.Domain.Entities;

using QuickIngestFile.Domain.Common;

/// <summary>
/// Import job entity to track import operations.
/// </summary>
public sealed class ImportJob : Entity
{
    public required string FileName { get; set; }
    public required string FileType { get; set; }
    public long FileSize { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int FailedRecords { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    public void Start()
    {
        Status = ImportStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(int totalRecords, int processedRecords, int failedRecords)
    {
        TotalRecords = totalRecords;
        ProcessedRecords = processedRecords;
        FailedRecords = failedRecords;
        Status = failedRecords > 0 ? ImportStatus.CompletedWithErrors : ImportStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ImportStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum ImportStatus
{
    Pending,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed
}
