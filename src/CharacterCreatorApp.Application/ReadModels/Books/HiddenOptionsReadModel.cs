namespace CharacterCreatorApp.Application.ReadModels.Books;

/// <summary>What a wizard list leaves out for the books not ticked: how many entries, and in which books.</summary>
public sealed class HiddenOptionsReadModel
{
    public int Count { get; init; }

    /// <summary>The books that hold any, in the order the books list gives them; none with a count of 0.</summary>
    public List<HiddenOptionsBookReadModel> Books { get; init; } = [];

    /// <summary>
    /// Counts the entries, by the book each is from (sources holds one per entry), under every
    /// book of the edition that isTicked leaves out. An entry of another edition's book is never
    /// counted, as no book of the edition holds it.
    /// </summary>
    public static HiddenOptionsReadModel Of(IEnumerable<BookReadModel> books, Func<string, bool> isTicked, IEnumerable<string> sources)
    {
        var counts = sources.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        var hidden = books
            .Where(b => !isTicked(b.Code))
            .Select(b => new HiddenOptionsBookReadModel { Code = b.Code, Name = b.Name, Count = counts.GetValueOrDefault(b.Code) })
            .Where(b => b.Count > 0)
            .ToList();

        return new HiddenOptionsReadModel { Count = hidden.Sum(b => b.Count), Books = hidden };
    }
}

public sealed class HiddenOptionsBookReadModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public int Count { get; init; }
}
