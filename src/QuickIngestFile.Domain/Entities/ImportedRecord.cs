namespace QuickIngestFile.Domain.Entities;

using System.Text.Json;
using QuickIngestFile.Domain.Common;

/// <summary>
/// Generic imported record that stores any data structure dynamically.
/// </summary>
public sealed class ImportedRecord : Entity
{
    public Guid ImportJobId { get; set; }
    public int RowNumber { get; set; }
    
    /// <summary>
    /// JSON serialized data from the imported row.
    /// </summary>
    public string DataJson { get; set; } = "{}";

    /// <summary>
    /// Get data as dictionary.
    /// </summary>
    public Dictionary<string, object?> GetData() =>
        JsonSerializer.Deserialize<Dictionary<string, object?>>(DataJson) ?? [];

    /// <summary>
    /// Set data from dictionary.
    /// </summary>
    public void SetData(Dictionary<string, object?> data) =>
        DataJson = JsonSerializer.Serialize(data, JsonOptions);

    /// <summary>
    /// Get typed value from data.
    /// </summary>
    public T? GetValue<T>(string key)
    {
        var data = GetData();
        if (!data.TryGetValue(key, out var value) || value is null)
            return default;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => (T)(object)element.GetString()!,
                JsonValueKind.Number when typeof(T) == typeof(int) => (T)(object)element.GetInt32(),
                JsonValueKind.Number when typeof(T) == typeof(long) => (T)(object)element.GetInt64(),
                JsonValueKind.Number when typeof(T) == typeof(double) => (T)(object)element.GetDouble(),
                JsonValueKind.Number when typeof(T) == typeof(decimal) => (T)(object)element.GetDecimal(),
                JsonValueKind.True or JsonValueKind.False => (T)(object)element.GetBoolean(),
                _ => default
            };
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
