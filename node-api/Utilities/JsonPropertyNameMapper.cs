using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;

namespace node_api.Utilities;

/// <summary>
/// Provides mapping from C# property names to JSON property names using JsonPropertyName attributes.
/// Results are cached for performance.
/// </summary>
public static class JsonPropertyNameMapper
{
    private static readonly ConcurrentDictionary<(Type, string), string> _cache = new();

    /// <summary>
    /// Gets the JSON property name for a C# property path.
    /// Returns the original property name if no JsonPropertyName attribute is found.
    /// </summary>
    /// <param name="type">The type containing the property</param>
    /// <param name="propertyPath">The property path (e.g., "Node" or "Nodes[0].Callsign")</param>
    /// <returns>The JSON property name</returns>
    public static string GetJsonPropertyName(Type type, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
            return propertyPath;

        // Check cache first
        var cacheKey = (type, propertyPath);
        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        // Handle indexed properties (e.g., "Nodes[0].Callsign")
        var parts = propertyPath.Split('.');
        var currentType = type;
        var jsonPath = new List<string>();

        foreach (var part in parts)
        {
            // Remove array indexer if present (e.g., "Nodes[0]" -> "Nodes")
            var propertyName = part.Contains('[') ? part[..part.IndexOf('[')] : part;
            var indexSuffix = part.Contains('[') ? part[part.IndexOf('[')..] : string.Empty;

            var property = currentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            if (property == null)
            {
                // Property not found, return original
                var result = propertyPath;
                _cache.TryAdd(cacheKey, result);
                return result;
            }

            // Get JSON property name from attribute
            var jsonAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var jsonPropName = jsonAttr?.Name ?? propertyName;
            jsonPath.Add(jsonPropName + indexSuffix);

            // Update current type for next iteration
            if (property.PropertyType.IsArray)
            {
                currentType = property.PropertyType.GetElementType()!;
            }
            else if (property.PropertyType.IsGenericType && 
                     property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                currentType = Nullable.GetUnderlyingType(property.PropertyType)!;
            }
            else
            {
                currentType = property.PropertyType;
            }
        }

        var mappedPath = string.Join(".", jsonPath);
        _cache.TryAdd(cacheKey, mappedPath);
        return mappedPath;
    }

    /// <summary>
    /// Clears the mapping cache. Useful for testing.
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
