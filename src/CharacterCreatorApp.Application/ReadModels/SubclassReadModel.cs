namespace CharacterCreatorApp.Application.ReadModels;

public sealed class SubclassReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ClassId { get; init; }

    /// <summary>A class whose whole spell list the subclass picks from, such as the Wizard's for an Eldritch Knight.</summary>
    public string? SpellListClassId { get; init; }

    public List<SpellProgressionReadModel> SpellProgression { get; init; } = [];
}
