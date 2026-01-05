namespace QuickIngestFile.Application.Parsing;

/// <summary>
/// Options for configuring file parsing behavior.
/// </summary>
public sealed class ParserOptions
{
    public char Delimiter { get; set; } = ';';
    public bool HasHeader { get; set; } = false;
    public int SkipRows { get; set; } = 0;
    public int BatchSize { get; set; } = 1000;
    public string? SheetName { get; set; }
    public int PreviewRows { get; set; } = 10;
}

/// <summary>
/// Result of parsing a row - generic with dictionary data.
/// </summary>
public sealed record ParsedRow(
    Dictionary<string, object?> Data,
    int RowNumber,
    bool IsSuccess,
    string? ErrorMessage = null);

/// <summary>
/// Detected schema from file analysis.
/// </summary>
public sealed record DetectedSchema(
    IReadOnlyList<DetectedColumn> Columns,
    int EstimatedRowCount);

/// <summary>
/// Detected column information.
/// </summary>
public sealed record DetectedColumn(
    string Name,
    int Index,
    string DetectedType);
