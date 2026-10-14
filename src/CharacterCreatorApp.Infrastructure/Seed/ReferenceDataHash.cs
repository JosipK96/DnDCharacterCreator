using System.Security.Cryptography;
using System.Text;

namespace CharacterCreatorApp.Infrastructure.Seed;

/// <summary>
/// Fingerprints the seed files, so the reference tables are replaced when any of them changes
/// and left alone otherwise.
/// </summary>
public static class ReferenceDataHash
{
    /// <summary>
    /// Part of the hash, for changes the files do not show: bump it when a seeder starts turning
    /// the same files into different rows, or when data still written in C# changes.
    /// </summary>
    public const int SeederVersion = 1;

    public static async Task<string> ComputeAsync(string dataPath, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.UTF8.GetBytes($"seeder-version:{SeederVersion}\n"));

        // Ordered by relative path with one separator, so the same files hash the same on
        // Windows and in the Linux container
        var files = Directory.EnumerateFiles(dataPath, "*.json", SearchOption.AllDirectories)
            .Select(path => (Path: path, Name: Path.GetRelativePath(dataPath, path).Replace('\\', '/')))
            .OrderBy(file => file.Name, StringComparer.Ordinal);

        foreach (var (path, name) in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes($"{name}\n"));
            hash.AppendData(await File.ReadAllBytesAsync(path, cancellationToken));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
