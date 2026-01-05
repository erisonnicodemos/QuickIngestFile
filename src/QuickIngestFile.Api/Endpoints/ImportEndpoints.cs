namespace QuickIngestFile.Api.Endpoints;

using Microsoft.AspNetCore.Mvc;
using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Application.Parsing;
using QuickIngestFile.Application.Services;

/// <summary>
/// Import-related endpoints for file upload and processing.
/// </summary>
public static class ImportEndpoints
{
    public static void MapImportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/import")
            .WithTags("Import")
            .DisableAntiforgery();

        // Preview file before importing
        group.MapPost("/preview", PreviewFile)
            .WithName("PreviewFile")
            .WithDescription("Preview file contents and detect schema before importing")
            .Produces<FilePreviewDto>(200)
            .Produces<ProblemDetails>(400);

        // Import file
        group.MapPost("/", ImportFile)
            .WithName("ImportFile")
            .WithDescription("Import file and store data")
            .Produces<ImportJobDto>(200)
            .Produces<ProblemDetails>(400);

        // Get supported formats
        group.MapGet("/formats", GetSupportedFormats)
            .WithName("GetSupportedFormats")
            .WithDescription("Get list of supported file formats");
    }

    private static async Task<IResult> PreviewFile(
        HttpRequest request,
        [FromServices] ImportService importService,
        [FromQuery] char delimiter = ';',
        [FromQuery] bool hasHeader = false,
        [FromQuery] int skipRows = 0,
        [FromQuery] string? sheetName = null)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Request must be multipart/form-data"
            });
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "No file provided",
                Detail = "Please upload a file"
            });
        }

        var options = new ParserOptions
        {
            Delimiter = delimiter,
            HasHeader = hasHeader,
            SkipRows = skipRows,
            SheetName = sheetName
        };

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await importService.GetPreviewAsync(
            stream, file.FileName, file.Length, options);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new ProblemDetails
            {
                Title = "Preview failed",
                Detail = result.Error
            });
    }

    private static async Task<IResult> ImportFile(
        HttpRequest request,
        [FromServices] ImportService importService,
        [FromQuery] char delimiter = ';',
        [FromQuery] bool hasHeader = false,
        [FromQuery] int skipRows = 0,
        [FromQuery] int batchSize = 1000,
        [FromQuery] string? sheetName = null)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Request must be multipart/form-data"
            });
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "No file provided",
                Detail = "Please upload a file"
            });
        }

        var options = new ParserOptions
        {
            Delimiter = delimiter,
            HasHeader = hasHeader,
            SkipRows = skipRows,
            BatchSize = batchSize,
            SheetName = sheetName
        };

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        var result = await importService.ImportAsync(
            stream, file.FileName, file.Length, options);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new ProblemDetails
            {
                Title = "Import failed",
                Detail = result.Error
            });
    }

    private static IResult GetSupportedFormats([FromServices] FileParserFactory parserFactory)
    {
        var extensions = parserFactory.GetSupportedExtensions();
        return Results.Ok(new
        {
            Formats = extensions,
            Description = new Dictionary<string, string>
            {
                [".csv"] = "Comma-Separated Values",
                [".tsv"] = "Tab-Separated Values",
                [".txt"] = "Text file (with delimiter)",
                [".xlsx"] = "Microsoft Excel (2007+)",
                [".xls"] = "Microsoft Excel (Legacy)"
            }
        });
    }
}
