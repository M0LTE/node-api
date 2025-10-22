using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace node_api.Utilities;

/// <summary>
/// Provides mapping from C# property names to JSON property names using JsonPropertyName attributes.
/// Results are cached for performance.
/// </summary>
public static partial class JsonPropertyNameMapper
{
    private static readonly ConcurrentDictionary<(Type, string), string> _cache = new();
    
    // Regex to find potential property names in error messages (PascalCase words)
    [GeneratedRegex(@"\b([A-Z][a-z]+(?:[A-Z][a-z]+)*)\b")]
    private static partial Regex PropertyNamePattern();

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
    /// Replaces C# property names in an error message with their JSON equivalents.
    /// This is useful for transforming validation error messages to use JSON property names.
    /// </summary>
    /// <param name="type">The type containing the properties</param>
    /// <param name="errorMessage">The error message potentially containing C# property names</param>
    /// <returns>The error message with C# property names replaced by JSON property names</returns>
    public static string TransformErrorMessage(Type type, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return errorMessage;

        // Get all properties of the type and nested types
        var propertyMap = BuildPropertyMap(type);

        // Replace each C# property name with its JSON equivalent
        // Sort by length (longest first) to avoid partial replacements
        foreach (var (csharpName, jsonName) in propertyMap.OrderByDescending(kvp => kvp.Key.Length))
        {
            if (csharpName != jsonName)
            {
                // Use word boundary to avoid partial matches
                errorMessage = Regex.Replace(
                    errorMessage,
                    $@"\b{Regex.Escape(csharpName)}\b",
                    jsonName,
                    RegexOptions.None);
            }
        }

        return errorMessage;
    }

    /// <summary>
    /// Builds a map of C# property names to JSON property names for a type and its nested types.
    /// </summary>
    private static Dictionary<string, string> BuildPropertyMap(Type type)
    {
        var map = new Dictionary<string, string>();
        BuildPropertyMapRecursive(type, map, new HashSet<Type>());
        return map;
    }

    private static void BuildPropertyMapRecursive(Type type, Dictionary<string, string> map, HashSet<Type> visitedTypes)
    {
        // Avoid infinite recursion
        if (visitedTypes.Contains(type))
            return;

        visitedTypes.Add(type);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var jsonAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
            var jsonName = jsonAttr?.Name ?? property.Name;
            
            if (!map.ContainsKey(property.Name))
            {
                map[property.Name] = jsonName;
            }

            // Recursively process nested types
            var propertyType = property.PropertyType;
            
            // Handle arrays
            if (propertyType.IsArray)
            {
                propertyType = propertyType.GetElementType()!;
            }
            // Handle nullable types
            else if (propertyType.IsGenericType && 
                     propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propertyType = Nullable.GetUnderlyingType(propertyType)!;
            }

            // Only process custom types, not primitives or system types
            if (propertyType.IsClass && 
                propertyType != typeof(string) && 
                !propertyType.IsAbstract &&
                !propertyType.Namespace?.StartsWith("System") == true)
            {
                BuildPropertyMapRecursive(propertyType, map, visitedTypes);
            }
        }
    }

    /// <summary>
    /// Clears the mapping cache. Useful for testing.
    /// </summary>
    public static void ClearCache()
    {
        _cache.Clear();
    }
}
