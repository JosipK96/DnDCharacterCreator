using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharacterCreatorApp.Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Configures Dictionary&lt;string, int&gt; to use PostgreSQL JSONB
    /// This is the recommended way for complex objects in PostgreSQL
    /// </summary>
    public static PropertyBuilder<Dictionary<string, int>> HasJsonConversion(this PropertyBuilder<Dictionary<string, int>> propertyBuilder)
    {
        var comparer = new ValueComparer<Dictionary<string, int>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
            c => c.ToDictionary(k => k.Key, v => v.Value));

        propertyBuilder
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>()
            )
            .Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }

    /// <summary>
    /// Generic JSON conversion for any complex type
    /// Uses PostgreSQL JSONB for better performance and queryability
    /// </summary>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new()
    {
        // Compares by content, as JSON, so a list changed in place is saved like a replaced one.
        // Without a comparer EF compared the list reference, missed in-place edits, and warned
        // about each such column every time it built the model.
        var comparer = new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => StringComparer.Ordinal.GetHashCode(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null)),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new T());

        propertyBuilder
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<T>(v, (JsonSerializerOptions?)null) ?? new T()
            )
            .Metadata.SetValueComparer(comparer);

        return propertyBuilder;
    }
}