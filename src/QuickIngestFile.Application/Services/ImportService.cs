namespace QuickIngestFile.Application.Services;

using System.Threading.Channels;
using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Application.Parsing;
using QuickIngestFile.Domain.Common;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// High-performance import service using Channels for producer/consumer pattern.
/// Handles any file type with dynamic schema detection.
/// </summary>
public sealed class ImportService(
    FileParserFactory parserFactory,
    IUnitOfWork unitOfWork)
{
    private const int ChannelCapacity = 10_000;
    private const int DefaultBatchSize = 1000;

    /// <summary>
    /// Get preview of file before importing.
    /// </summary>
    public async Task<Result<FilePreviewDto>> GetPreviewAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        ParserOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            options ??= new ParserOptions();
            var parser = parserFactory.GetParser(fileName);

            var schema = await parser.DetectSchemaAsync(fileStream, options, cancellationToken);
            var previewRows = await parser.GetPreviewAsync(fileStream, options, cancellationToken);

            var columns = schema.Columns.Select(c => new ColumnDefinitionDto(
                c.Name,
                c.Index,
                c.DetectedType)).ToList();

            var rows = previewRows
                .Where(r => r.IsSuccess)
                .Select(r => r.Data)
                .ToList();

            return Result.Success(new FilePreviewDto(
                fileName,
                fileSize,
                columns,
                rows,
                schema.EstimatedRowCount));
        }
        catch (Exception ex)
        {
            return Result.Failure<FilePreviewDto>($"Failed to preview file: {ex.Message}");
        }
    }

    /// <summary>
    /// Import data from a file stream with high performance.
    /// </summary>
    public async Task<Result<ImportJobDto>> ImportAsync(
        Stream fileStream,
        string fileName,
        long fileSize,
        ParserOptions? options = null,
        IProgress<ImportProgressDto>? progress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ParserOptions();
        var parser = parserFactory.GetParser(fileName);
        var fileType = Path.GetExtension(fileName).TrimStart('.');

        // Create import job
        var importJob = new ImportJob
        {
            FileName = fileName,
            FileType = fileType,
            FileSize = fileSize
        };

        await unitOfWork.ImportJobs.AddAsync(importJob, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            // Detect and save schema
            var schema = await parser.DetectSchemaAsync(fileStream, options, cancellationToken);
            var fileSchema = new FileSchema
            {
                ImportJobId = importJob.Id,
                FileName = fileName
            };
            fileSchema.SetColumns(schema.Columns.Select(c => new ColumnDefinition
            {
                Name = c.Name,
                Index = c.Index,
                DetectedType = c.DetectedType
            }));

            await unitOfWork.FileSchemas.AddAsync(fileSchema, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            importJob.TotalRecords = schema.EstimatedRowCount;
            importJob.Start();

            // Process import with channels
            var result = await ProcessImportAsync(
                parser, fileStream, options, importJob, progress, cancellationToken);

            importJob.Complete(result.Total, result.Processed, result.Failed);
        }
        catch (Exception ex)
        {
            importJob.Fail(ex.Message);
            await unitOfWork.ImportJobs.UpdateAsync(importJob, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<ImportJobDto>($"Import failed: {ex.Message}");
        }

        await unitOfWork.ImportJobs.UpdateAsync(importJob, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ImportJobDto.FromEntity(importJob));
    }

    private async Task<(int Total, int Processed, int Failed)> ProcessImportAsync(
        IFileParser parser,
        Stream fileStream,
        ParserOptions options,
        ImportJob importJob,
        IProgress<ImportProgressDto>? progress,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<ImportedRecord>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });

        var totalRecords = 0;
        var processedRecords = 0;
        var failedRecords = 0;

        // Producer: Parse file and write to channel
        var producerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var row in parser.ParseAsync(fileStream, options, cancellationToken))
                {
                    totalRecords++;

                    if (row.IsSuccess)
                    {
                        var record = new ImportedRecord
                        {
                            ImportJobId = importJob.Id,
                            RowNumber = row.RowNumber
                        };
                        record.SetData(row.Data);

                        await channel.Writer.WriteAsync(record, cancellationToken);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedRecords);
                    }
                }
            }
            finally
            {
                channel.Writer.Complete();
            }
        }, cancellationToken);

        // Consumer: Read from channel and batch insert
        var batchSize = options.BatchSize > 0 ? options.BatchSize : DefaultBatchSize;
        var consumerTask = Task.Run(async () =>
        {
            var batch = new List<ImportedRecord>(batchSize);

            await foreach (var record in channel.Reader.ReadAllAsync(cancellationToken))
            {
                batch.Add(record);

                if (batch.Count >= batchSize)
                {
                    await unitOfWork.ImportedRecords.BulkInsertAsync(batch, cancellationToken);
                    Interlocked.Add(ref processedRecords, batch.Count);

                    ReportProgress(progress, importJob.Id, processedRecords, importJob.TotalRecords);

                    batch.Clear();
                }
            }

            // Insert remaining items
            if (batch.Count > 0)
            {
                await unitOfWork.ImportedRecords.BulkInsertAsync(batch, cancellationToken);
                Interlocked.Add(ref processedRecords, batch.Count);
                ReportProgress(progress, importJob.Id, processedRecords, importJob.TotalRecords);
            }
        }, cancellationToken);

        await Task.WhenAll(producerTask, consumerTask);

        return (totalRecords, processedRecords, failedRecords);
    }

    private static void ReportProgress(
        IProgress<ImportProgressDto>? progress,
        Guid jobId,
        int processed,
        int total)
    {
        if (progress is null || total == 0)
            return;

        var percentage = (double)processed / total * 100;
        progress.Report(new ImportProgressDto(
            jobId,
            processed,
            total,
            Math.Round(percentage, 2),
            "Processing"));
    }
}
