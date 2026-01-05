namespace QuickIngestFile.Api.Endpoints;

using Microsoft.AspNetCore.Mvc;
using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Application.Services;
using QuickIngestFile.Domain.Common;

/// <summary>
/// Data query endpoints for accessing imported records.
/// </summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/data")
            .WithTags("Data");

        // Get schema for import job
        group.MapGet("/{importJobId:guid}/schema", GetSchema)
            .WithName("GetSchema")
            .WithDescription("Get schema/columns for an import job")
            .Produces<FileSchemaDto>(200)
            .Produces<ProblemDetails>(404);

        // Get records for import job
        group.MapGet("/{importJobId:guid}/records", GetRecords)
            .WithName("GetRecords")
            .WithDescription("Get paginated records for an import job")
            .Produces<PagedResult<ImportedRecordDto>>(200);

        // Search records
        group.MapGet("/{importJobId:guid}/search", SearchRecords)
            .WithName("SearchRecords")
            .WithDescription("Search records by value")
            .Produces<IReadOnlyList<ImportedRecordDto>>(200);

        // Get record count
        group.MapGet("/{importJobId:guid}/count", GetRecordCount)
            .WithName("GetRecordCount")
            .WithDescription("Get total record count for an import job")
            .Produces<int>(200);

        // Delete import data
        group.MapDelete("/{importJobId:guid}", DeleteImportData)
            .WithName("DeleteImportData")
            .WithDescription("Delete all data for an import job")
            .Produces(204);
    }

    private static async Task<IResult> GetSchema(
        Guid importJobId,
        [FromServices] DataQueryService dataService)
    {
        var result = await dataService.GetSchemaAsync(importJobId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new ProblemDetails
            {
                Title = "Schema not found",
                Detail = result.Error
            });
    }

    private static async Task<IResult> GetRecords(
        Guid importJobId,
        [FromServices] DataQueryService dataService,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await dataService.GetRecordsAsync(importJobId, page, pageSize);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchRecords(
        Guid importJobId,
        [FromServices] DataQueryService dataService,
        [FromQuery] string q = "")
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Search term required",
                Detail = "Please provide a search term using the 'q' query parameter"
            });
        }

        var result = await dataService.SearchRecordsAsync(importJobId, q);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecordCount(
        Guid importJobId,
        [FromServices] DataQueryService dataService)
    {
        var count = await dataService.GetRecordCountAsync(importJobId);
        return Results.Ok(new { Count = count });
    }

    private static async Task<IResult> DeleteImportData(
        Guid importJobId,
        [FromServices] DataQueryService dataService)
    {
        await dataService.DeleteImportDataAsync(importJobId);
        return Results.NoContent();
    }
}
