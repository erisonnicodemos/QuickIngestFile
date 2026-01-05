namespace QuickIngestFile.Application.Parsing;

/// <summary>
/// Interface for file parsers with Strategy Pattern support.
/// Parses any file format into dynamic key-value data.
/// </summary>
public interface IFileParser
{
    /// <summary>
    /// File extensions supported by this parser.
    /// </summary>
    string[] SupportedExtensions { get; }

    /// <summary>
    /// Check if this parser supports the given file.
    /// </summary>
    bool CanParse(string fileName);

    /// <summary>
    /// Analyze file and detect schema (columns and types).
    /// </summary>
    Task<DetectedSchema> DetectSchemaAsync(
        Stream stream,
        ParserOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get preview rows from file.
    /// </summary>
    Task<IReadOnlyList<ParsedRow>> GetPreviewAsync(
        Stream stream,
        ParserOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse file stream and yield records asynchronously.
    /// </summary>
    IAsyncEnumerable<ParsedRow> ParseAsync(
        Stream stream,
        ParserOptions options,
        CancellationToken cancellationToken = default);
}
