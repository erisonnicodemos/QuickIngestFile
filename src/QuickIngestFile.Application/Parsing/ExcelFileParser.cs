namespace QuickIngestFile.Application.Parsing;

using System.Runtime.CompilerServices;
using ClosedXML.Excel;
using QuickIngestFile.Domain.Entities;

/// <summary>
/// Excel file parser using ClosedXML library.
/// Parses Excel files into dynamic dictionary-based records.
/// </summary>
public sealed class ExcelFileParser : IFileParser
{
    public string[] SupportedExtensions => [".xlsx", ".xls"];

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
        await Task.Yield();

        using var workbook = new XLWorkbook(stream);
        var worksheet = GetWorksheet(workbook, options.SheetName);

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
            return new DetectedSchema([], 0);

        var lastColumn = usedRange.LastColumn().ColumnNumber();
        var lastRow = usedRange.LastRow().RowNumber();

        var columns = new List<DetectedColumn>();
        var sampleValues = new Dictionary<int, List<string>>();

        // Get column names from first row if has header
        var headerRow = options.SkipRows + 1;
        for (var col = 1; col <= lastColumn; col++)
        {
            var columnName = options.HasHeader
                ? worksheet.Cell(headerRow, col).GetString().Trim()
                : $"Column{col}";

            if (string.IsNullOrWhiteSpace(columnName))
                columnName = $"Column{col}";

            columns.Add(new DetectedColumn(columnName, col - 1, DataTypes.String));
            sampleValues[col - 1] = [];
        }

        // Sample first 100 data rows for type detection
        var startRow = headerRow + (options.HasHeader ? 1 : 0);
        var sampleEnd = Math.Min(startRow + 100, lastRow);

        for (var row = startRow; row <= sampleEnd; row++)
        {
            for (var col = 1; col <= lastColumn; col++)
            {
                var cell = worksheet.Cell(row, col);
                if (!cell.IsEmpty())
                {
                    sampleValues[col - 1].Add(cell.GetString());
                }
            }
        }

        // Detect types based on samples
        var typedColumns = columns.Select(c =>
        {
            var samples = sampleValues[c.Index];
            var detectedType = DetectColumnType(samples);
            return c with { DetectedType = detectedType };
        }).ToList();

        var dataRowCount = lastRow - startRow + 1;

        stream.Position = 0;

        return new DetectedSchema(typedColumns, Math.Max(0, dataRowCount));
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
        await Task.Yield();

        using var workbook = new XLWorkbook(stream);
        var worksheet = GetWorksheet(workbook, options.SheetName);

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
            yield break;

        var lastColumn = usedRange.LastColumn().ColumnNumber();
        var lastRow = usedRange.LastRow().RowNumber();

        // Get column names
        var headerRow = options.SkipRows + 1;
        var columnNames = new string[lastColumn];
        
        for (var col = 1; col <= lastColumn; col++)
        {
            var columnName = options.HasHeader
                ? worksheet.Cell(headerRow, col).GetString().Trim()
                : $"Column{col}";

            if (string.IsNullOrWhiteSpace(columnName))
                columnName = $"Column{col}";

            columnNames[col - 1] = columnName;
        }

        var startRow = headerRow + (options.HasHeader ? 1 : 0);
        var rowNumber = 0;

        for (var row = startRow; row <= lastRow; row++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            rowNumber++;
            Dictionary<string, object?>? data = null;
            string? errorMessage = null;

            try
            {
                data = [];
                for (var col = 1; col <= lastColumn; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    data[columnNames[col - 1]] = GetCellValue(cell);
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

    private static IXLWorksheet GetWorksheet(XLWorkbook workbook, string? sheetName)
    {
        return string.IsNullOrEmpty(sheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(sheetName);
    }

    private static string DetectColumnType(List<string> samples)
    {
        if (samples.Count == 0)
            return DataTypes.String;

        var types = samples.Select(DataTypes.Detect).ToList();

        var mostCommon = types
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        var percentage = types.Count(t => t == mostCommon) / (double)types.Count;
        return percentage >= 0.8 ? mostCommon : DataTypes.String;
    }

    private static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
            return null;

        return cell.DataType switch
        {
            XLDataType.Boolean => cell.GetBoolean(),
            XLDataType.Number => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.TimeSpan => cell.GetTimeSpan(),
            _ => cell.GetString().Trim()
        };
    }
}
