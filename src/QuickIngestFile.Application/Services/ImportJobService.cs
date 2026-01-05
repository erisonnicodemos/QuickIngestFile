namespace QuickIngestFile.Application.Services;

using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// Service for managing import jobs.
/// </summary>
public sealed class ImportJobService(IUnitOfWork unitOfWork)
{
    /// <summary>
    /// Get recent import jobs.
    /// </summary>
    public async Task<IReadOnlyList<ImportJobDto>> GetRecentJobsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var jobs = await unitOfWork.ImportJobs.GetRecentAsync(count, cancellationToken);
        return jobs.Select(ImportJobDto.FromEntity).ToList();
    }

    /// <summary>
    /// Get import job by ID.
    /// </summary>
    public async Task<Result<ImportJobDto>> GetJobByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var job = await unitOfWork.ImportJobs.GetByIdAsync(id, cancellationToken);

        return job is null
            ? Result.Failure<ImportJobDto>($"Import job with ID {id} not found")
            : Result.Success(ImportJobDto.FromEntity(job));
    }

    /// <summary>
    /// Get all import jobs with pagination.
    /// </summary>
    public async Task<PagedResult<ImportJobDto>> GetJobsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await unitOfWork.ImportJobs.GetPagedAsync(page, pageSize, cancellationToken);

        var dtos = result.Items.Select(ImportJobDto.FromEntity).ToList();

        return new PagedResult<ImportJobDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }

    /// <summary>
    /// Delete an import job and its data.
    /// </summary>
    public async Task<Result> DeleteJobAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var job = await unitOfWork.ImportJobs.GetByIdAsync(id, cancellationToken);

        if (job is null)
            return Result.Failure($"Import job with ID {id} not found");

        // Delete related data
        await unitOfWork.ImportedRecords.DeleteByImportJobIdAsync(id, cancellationToken);
        await unitOfWork.ImportJobs.DeleteAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
