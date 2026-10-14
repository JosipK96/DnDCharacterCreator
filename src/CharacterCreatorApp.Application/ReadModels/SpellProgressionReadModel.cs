namespace CharacterCreatorApp.Application.ReadModels;

public sealed class SpellProgressionReadModel
{
    public int CharacterLevel { get; init; }
    public int SpellsKnown { get; init; }
    public int CantripsKnown { get; init; }
    public int MaxSpellLevel { get; init; }
    public string? SpellcastingAbility { get; init; } // only set for subclass progressions
}