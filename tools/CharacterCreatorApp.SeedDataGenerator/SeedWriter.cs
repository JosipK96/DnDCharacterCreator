using System.Text.Encodings.Web;
using System.Text.Json;

namespace CharacterCreatorApp.SeedDataGenerator;

/// <summary>
/// Writes a seed file and says which ids it gained and lost against the version it replaces. A
/// lost race, subrace, class, subclass or background matters most: a character that has it stops
/// the app from loading the new data (see SeedService).
/// </summary>
internal static class SeedWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,

        // Apostrophes and dashes written as themselves rather than as \u escapes
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // childIds are the ids of what each row holds inside it, such as a race's subraces, reported
    // the same way
    public static async Task WriteAsync<T>(
        string dataPath,
        string fileName,
        IReadOnlyCollection<T> rows,
        Func<T, string> id,
        Func<T, IEnumerable<string>>? childIds = null)
    {
        var path = Path.Combine(dataPath, fileName);
        var previous = await ReadRowsAsync<T>(path);
        var ids = rows.Select(id).ToList();

        var duplicates = ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"{fileName} would have these ids twice: {string.Join(", ", duplicates)}");
        }

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(rows, Options) + Environment.NewLine);

        Compare(fileName, rows.Count, previous.Select(id).ToList(), ids, "rows");

        if (childIds != null)
        {
            Compare($"  inside {fileName}", null, previous.SelectMany(childIds).ToList(), rows.SelectMany(childIds).ToList(), "ids");
        }
    }

    private static void Compare(string label, int? count, List<string> previousIds, List<string> ids, string noun)
    {
        var added = ids.Except(previousIds).ToList();
        var removed = previousIds.Except(ids).ToList();

        Console.WriteLine($"{label}: {count ?? ids.Count} {noun}, {added.Count} new, {removed.Count} gone");
        Report("  gone", removed);
        Report("  new", added);
    }

    // The rows in the file being replaced. A file in an older shape reads as having none, and
    // every row counts as new.
    private static async Task<List<T>> ReadRowsAsync<T>(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(await File.ReadAllTextAsync(path), Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void Report(string label, List<string> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        const int Shown = 40;
        var more = ids.Count > Shown ? $" and {ids.Count - Shown} more" : string.Empty;
        Console.WriteLine($"{label}: {string.Join(", ", ids.Take(Shown))}{more}");
    }
}
