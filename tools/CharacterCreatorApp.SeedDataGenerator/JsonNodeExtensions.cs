using System.Text.Json.Nodes;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>Lenient reads: 5etools leaves out whatever does not apply, so a missing key is normal.</summary>
internal static class JsonNodeExtensions
{
    public static string? Str(this JsonNode? node, string key) =>
        (node as JsonObject)?[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static int? Int(this JsonNode? node, string key) =>
        (node as JsonObject)?[key] is JsonValue value && value.TryGetValue<int>(out var number) ? number : null;

    public static bool Bool(this JsonNode? node, string key) =>
        (node as JsonObject)?[key] is JsonValue value && value.TryGetValue<bool>(out var flag) && flag;

    public static JsonArray Arr(this JsonNode? node, string key) =>
        (node as JsonObject)?[key] as JsonArray ?? [];

    public static IEnumerable<JsonObject> Objects(this JsonNode? node, string key) =>
        node.Arr(key).OfType<JsonObject>();

    /// <summary>The array's numbers, or an empty list when the key is missing.</summary>
    public static List<int> Ints(this JsonNode? node, string key) =>
        node.Arr(key).Select(n => n is JsonValue v && v.TryGetValue<int>(out var i) ? i : 0).ToList();

    public static string? AsString(this JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
