namespace QuickIngestFile.Application.Parsing;

using System.Runtime.CompilerServices;
using QuickIngestFile.Domain.Entities;
using Sylvan.Data.Csv;

/// <summary>
/// High-performance CSV parser using Sylvan library.
/// Parses CSV files into dynamic dictionary-based records.
/// </summary>
public sealed class CsvFileParser : IFileParser
{
    public string[] SupportedExtensions => [".csv", ".tsv", ".txt"];

    public bool CanParse(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<DetectedSchema> DetectSchemaAsync(
        Stream stream,
        ParserOptions options,
        CancellationToken cancellationToken = default)
    {
        var csvOptions = new CsvDataReaderOptions
        {
            Delimiter = options.Delimiter,
            HasHeaders = options.HasHeader
        };

        using var reader = new StreamReader(stream, leaveOpen: true);
        await using var csv = await CsvDataReader.CreateAsync(reader, csvOptions);

        var columns = new List<DetectedColumn>();
        var sampleValues = new Dictionary<int, List<string>>();

        // Get column names
        for (var i = 0; i < csv.FieldCount; i++)
        {
            var columnName = options.HasHeader 
                ? csv.GetName(i) 
                : $"Column{i + 1}";
            
            columns.Add(new DetectedColumn(columnName, i, DataTypes.String));
            sampleValues[i] = [];
        }

        // Sample first 100 rows for type detection
        var rowCount = 0;
        while (await csv.ReadAsync(cancellationToken) && rowCount < 100)
        {
            for (var i = 0; i < csv.FieldCount; i++)
            {
                var value = csv.GetString(i);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    sampleValues[i].Add(value);
                }
            }
            rowCount++;
        }

        // Count remaining rows
        while (await csv.ReadAsync(cancellationToken))
        {
            rowCount++;
        }

        // Detect types based on samples
        var typedColumns = columns.Select(c =>
        {
            var samples = sampleValues[c.Index];
            var detectedType = DetectColumnType(samples);
            return c with { DetectedType = detectedType };
        }).ToList();

        stream.Position = 0;

        return new DetectedSchema(typedColumns, rowCount);
    }

    public async Task<IReadOnlyList<ParsedRow>> GetPreviewAsync(
        Stream stream,
        ParserOptions options,
        CancellationToken cancellationToken = default)
    {
        var rows = new List<ParsedRow>();
        var count = 0;

        await foreach (var row in ParseAsync(stream, options, cancellationToken))
        {
            rows.Add(row);
            count++;
            if (count >= options.PreviewRows)
                break;
        }

        stream.Position = 0;
        return rows;
    }

    public async IAsyncEnumerable<ParsedRow> ParseAsync(
        Stream stream,
        ParserOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var csvOptions = new CsvDataReaderOptions
        {
            Delimiter = options.Delimiter,
            HasHeaders = options.HasHeader
        };

        using var reader = new StreamReader(stream, leaveOpen: true);
        await using var csv = await CsvDataReader.CreateAsync(reader, csvOptions);

        // Get column names
        var columnNames = new string[csv.FieldCount];
        for (var i = 0; i < csv.FieldCount; i++)
        {
            columnNames[i] = options.HasHeader 
                ? csv.GetName(i) 
                : $"Column{i + 1}";
        }

        // Skip rows if configured
        for (var i = 0; i < options.SkipRows && await csv.ReadAsync(cancellationToken); i++) { }

        var rowNumber = 0;
        while (await csv.ReadAsync(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            rowNumber++;
            Dictionary<string, object?>? data = null;
            string? errorMessage = null;

            try
            {
                data = [];
                for (var i = 0; i < csv.FieldCount; i++)
                {
                    var value = csv.GetString(i);
                    data[columnNames[i]] = ParseValue(value);
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Row {rowNumber}: {ex.Message}";
            }

            yield return new ParsedRow(
                data ?? [],
                rowNumber,
                data is not null,
                errorMessage);
        }
    }

    private static string DetectColumnType(List<string> samples)
    {
        if (samples.Count == 0)
            return DataTypes.String;

        var types = samples.Select(DataTypes.Detect).ToList();
        
        // If all samples are the same type, use that type
        var mostCommon = types
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        // If more than 80% are the same type, use it
        var percentage = types.Count(t => t == mostCommon) / (double)types.Count;
        return percentage >= 0.8 ? mostCommon : DataTypes.String;
    }

    private static object? ParseValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Try parsing in order of specificity
        if (int.TryParse(value, out var intVal))
            return intVal;

        if (decimal.TryParse(value, out var decVal))
            return decVal;

        if (bool.TryParse(value, out var boolVal))
            return boolVal;

        if (DateTime.TryParse(value, out var dateVal))
            return dateVal;

        return value;
    }
}
