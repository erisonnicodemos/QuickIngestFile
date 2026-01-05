namespace QuickIngestFile.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickIngestFile.Application.DTOs;
using QuickIngestFile.Application.Parsing;
using QuickIngestFile.Domain.Entities;
using QuickIngestFile.Domain.Repositories;

/// <summary>
/// Background worker that processes import jobs from the queue.
/// Supports parallel processing of multiple imports.
/// </summary>
public sealed class ImportBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BackgroundImportQueue _queue;
    private readonly ILogger<ImportBackgroundWorker> _logger;
    
    // Maximum concurrent imports - adjust based on resources
    private const int MaxConcurrentImports = 3;
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentImports);
    
    public ImportBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        BackgroundImportQueue queue,
        ILogger<ImportBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Import Background Worker started. Max concurrent imports: {Max}", MaxConcurrentImports);
        
        var runningTasks = new List<Task>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Clean up completed tasks
                runningTasks.RemoveAll(t => t.IsCompleted);
                
                // Wait for semaphore slot
                await _semaphore.WaitAsync(stoppingToken);
                
                // Dequeue next job
                var job = await _queue.DequeueAsync(stoppingToken);
                
                _logger.LogInformation(
                    "Processing import job {JobId} for file {FileName}. Pending: {Pending}, Running: {Running}",
                    job.ImportJobId, job.FileName, _queue.PendingCount, MaxConcurrentImports - _semaphore.CurrentCount);
                
                // Start processing in background
                var task = ProcessJobAsync(job, stoppingToken);
                runningTasks.Add(task);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Import Background Worker stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background worker main loop");
                _semaphore.Release();
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        // Wait for all running tasks to complete
        if (runningTasks.Any())
        {
            _logger.LogInformation("Waiting for {Count} running imports to complete...", runningTasks.Count);
            await Task.WhenAll(runningTasks);
        }
        
        _logger.LogInformation("Import Background Worker stopped.");
    }
    
    private async Task ProcessJobAsync(QueuedImportJob job, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var parserFactory = scope.ServiceProvider.GetRequiredService<FileParserFactory>();
            
            // Get the import job from database
            var importJob = await unitOfWork.ImportJobs.GetByIdAsync(job.ImportJobId, stoppingToken);
            if (importJob is null)
            {
                _logger.LogWarning("Import job {JobId} not found in database", job.ImportJobId);
                return;
            }
            
            try
            {
                var parser = parserFactory.GetParser(job.FileName);
                using var stream = new MemoryStream(job.FileData);
                
                // Detect and save schema
                var schema = await parser.DetectSchemaAsync(stream, job.Options, stoppingToken);
                var fileSchema = new FileSchema
                {
                    ImportJobId = importJob.Id,
                    FileName = job.FileName
                };
                fileSchema.SetColumns(schema.Columns.Select(c => new ColumnDefinition
                {
                    Name = c.Name,
                    Index = c.Index,
                    DetectedType = c.DetectedType
                }));

                await unitOfWork.FileSchemas.AddAsync(fileSchema, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                importJob.TotalRecords = schema.EstimatedRowCount;
                importJob.Start();
                await unitOfWork.ImportJobs.UpdateAsync(importJob, stoppingToken);
                await unitOfWork.SaveChangesAsync(stoppingToken);

                // Reset stream position for parsing
                stream.Position = 0;
                
                // Process with producer/consumer pattern
                var result = await ProcessImportAsync(
                    parser, stream, job.Options, importJob, unitOfWork, stoppingToken);

                importJob.Complete(result.Total, result.Processed, result.Failed);
                
                _logger.LogInformation(
                    "Import job {JobId} completed. Total: {Total}, Processed: {Processed}, Failed: {Failed}",
                    job.ImportJobId, result.Total, result.Processed, result.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import job {JobId} failed: {Message}", job.ImportJobId, ex.Message);
                importJob.Fail(ex.Message);
            }

            await unitOfWork.ImportJobs.UpdateAsync(importJob, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private async Task<(int Total, int Processed, int Failed)> ProcessImportAsync(
        IFileParser parser,
        Stream fileStream,
        ParserOptions options,
        ImportJob importJob,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        const int channelCapacity = 10_000;
        const int defaultBatchSize = 1000;
        
        var channel = System.Threading.Channels.Channel.CreateBounded<ImportedRecord>(
            new System.Threading.Channels.BoundedChannelOptions(channelCapacity)
            {
                FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait,
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
        var batchSize = options.BatchSize > 0 ? options.BatchSize : defaultBatchSize;
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
                    
                    // Update job progress periodically
                    importJob.ProcessedRecords = processedRecords;
                    await unitOfWork.ImportJobs.UpdateAsync(importJob, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    batch.Clear();
                }
            }

            // Insert remaining items
            if (batch.Count > 0)
            {
                await unitOfWork.ImportedRecords.BulkInsertAsync(batch, cancellationToken);
                Interlocked.Add(ref processedRecords, batch.Count);
                
                importJob.ProcessedRecords = processedRecords;
                await unitOfWork.ImportJobs.UpdateAsync(importJob, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        await Task.WhenAll(producerTask, consumerTask);

        return (totalRecords, processedRecords, failedRecords);
    }
}
