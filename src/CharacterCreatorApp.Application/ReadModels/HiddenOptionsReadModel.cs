namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>What a wizard list leaves out for the books not ticked: how many entries, and in which books.</summary>
public sealed class HiddenOptionsReadModel
{
    public int Count { get; init; }

    /// <summary>The books that hold any, in the order the books list gives them; none with a count of 0.</summary>
    public List<HiddenOptionsBookReadModel> Books { get; init; } = [];
}

public sealed class HiddenOptionsBookReadModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public int Count { get; init; }
}
