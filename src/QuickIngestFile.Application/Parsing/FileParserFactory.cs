namespace QuickIngestFile.Application.Parsing;

/// <summary>
/// Factory for resolving the appropriate file parser based on file extension.
/// </summary>
public sealed class FileParserFactory(IEnumerable<IFileParser> parsers)
{
    private readonly IFileParser[] _parsers = parsers.ToArray();

    /// <summary>
    /// Get the appropriate parser for the given file.
    /// </summary>
    public IFileParser GetParser(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileName));

        return parser ?? throw new NotSupportedException(
            $"No parser available for file type: {extension}. " +
            $"Supported types: {string.Join(", ", _parsers.SelectMany(p => p.SupportedExtensions))}");
    }

    /// <summary>
    /// Check if any parser supports the given file.
    /// </summary>
    public bool CanParse(string fileName) =>
        _parsers.Any(p => p.CanParse(fileName));

    /// <summary>
    /// Get all supported file extensions.
    /// </summary>
    public string[] GetSupportedExtensions() =>
        _parsers.SelectMany(p => p.SupportedExtensions).Distinct().ToArray();
}
