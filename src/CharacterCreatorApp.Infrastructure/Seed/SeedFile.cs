using System.Text.Json;

namespace CharacterCreatorApp.Infrastructure.Seed;

/// <summary>
/// Reads a seed-data file. The files hold plain text: the generator takes 5etools' markup out
/// ("{@damage 1d8}" is "1d8") before it writes them, and SeedDataIntegrityTests checks none is
/// left, so what is read goes into the database as it is.
/// </summary>
public static class SeedFile
{
    /// <summary>
    /// Every seed file lists at least one item, so an empty one means a broken file; seeding
    /// stops rather than leave a table silently empty.
    /// </summary>
    public static async Task<List<T>> ReadListAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var items = await JsonSerializer.DeserializeAsync<List<T>>(stream, cancellationToken: cancellationToken);

        if (items is null || items.Count == 0)
        {
            throw new InvalidOperationException($"{Path.GetFileName(path)} is empty or invalid.");
        }

        return items;
    }
}
