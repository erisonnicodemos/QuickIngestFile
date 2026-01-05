namespace QuickIngestFile.Domain.Entities;

using System.Text.Json;
using QuickIngestFile.Domain.Common;

/// <summary>
/// Represents the schema/structure detected from an imported file.
/// </summary>
public sealed class FileSchema : Entity
{
    public Guid ImportJobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized column definitions.
    /// </summary>
    public string ColumnsJson { get; set; } = "[]";

    /// <summary>
    /// Get columns as list.
    /// </summary>
    public List<ColumnDefinition> GetColumns() =>
        JsonSerializer.Deserialize<List<ColumnDefinition>>(ColumnsJson, JsonOptions) ?? [];

    /// <summary>
    /// Set columns.
    /// </summary>
    public void SetColumns(IEnumerable<ColumnDefinition> columns) =>
        ColumnsJson = JsonSerializer.Serialize(columns.ToList(), JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Definition of a column in the imported file.
/// </summary>
public sealed record ColumnDefinition
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public required string DetectedType { get; init; }
    public string? DisplayName { get; init; }
    public bool IsIgnored { get; init; } = false;
}

/// <summary>
/// Supported data types for columns.
/// </summary>
public static class DataTypes
{
    public const string String = "string";
    public const string Integer = "integer";
    public const string Decimal = "decimal";
    public const string Boolean = "boolean";
    public const string DateTime = "datetime";
    public const string Date = "date";
    public const string Unknown = "unknown";

    public static string Detect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return String;

        if (int.TryParse(value, out _))
            return Integer;

        if (decimal.TryParse(value, out _))
            return Decimal;

        if (bool.TryParse(value, out _))
            return Boolean;

        if (System.DateTime.TryParse(value, out _))
            return DateTime;

        if (DateOnly.TryParse(value, out _))
            return Date;

        return String;
    }
}
