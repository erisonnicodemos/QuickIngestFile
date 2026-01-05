namespace QuickIngestFile.Api.Endpoints;

using Microsoft.AspNetCore.Mvc;
using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Application.Services;
using QuickIngestFile.Domain.Common;

/// <summary>
/// Import job management endpoints.
/// </summary>
public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        // Get recent jobs
        group.MapGet("/recent", GetRecentJobs)
            .WithName("GetRecentJobs")
            .WithDescription("Get recent import jobs")
            .Produces<IReadOnlyList<ImportJobDto>>(200);

        // Get all jobs (paginated)
        group.MapGet("/", GetJobs)
            .WithName("GetJobs")
            .WithDescription("Get all import jobs with pagination")
            .Produces<PagedResult<ImportJobDto>>(200);

        // Get job by ID
        group.MapGet("/{id:guid}", GetJobById)
            .WithName("GetJobById")
            .WithDescription("Get import job details by ID")
            .Produces<ImportJobDto>(200)
            .Produces<ProblemDetails>(404);

        // Delete job
        group.MapDelete("/{id:guid}", DeleteJob)
            .WithName("DeleteJob")
            .WithDescription("Delete an import job and all its data")
            .Produces(204)
            .Produces<ProblemDetails>(404);
    }

    private static async Task<IResult> GetRecentJobs(
        [FromServices] ImportJobService jobService,
        [FromQuery] int count = 10)
    {
        var jobs = await jobService.GetRecentJobsAsync(count);
        return Results.Ok(jobs);
    }

    private static async Task<IResult> GetJobs(
        [FromServices] ImportJobService jobService,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await jobService.GetJobsAsync(page, pageSize);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetJobById(
        Guid id,
        [FromServices] ImportJobService jobService)
    {
        var result = await jobService.GetJobByIdAsync(id);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new ProblemDetails
            {
                Title = "Job not found",
                Detail = result.Error
            });
    }

    private static async Task<IResult> DeleteJob(
        Guid id,
        [FromServices] ImportJobService jobService)
    {
        var result = await jobService.DeleteJobAsync(id);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(new ProblemDetails
            {
                Title = "Job not found",
                Detail = result.Error
            });
    }
}
