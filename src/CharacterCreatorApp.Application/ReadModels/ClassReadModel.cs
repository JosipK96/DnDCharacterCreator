namespace CharacterCreatorApp.Application.ReadModels;

public sealed class ClassReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Edition { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
    public string Source { get; init; } = string.Empty;

    public required string HitDie { get; init; }
    public int SubclassLevel { get; init; }
    public List<int> AsiLevels { get; init; } = [];
    public List<string> SavingThrowAbilityIds { get; init; } = [];
    public List<ProficiencyReadModel> ClassProficiencies { get; init; } = [];
    public ProficiencyChoiceReadModel? ProficiencyChoice { get; init; }
    public SpellcastingReadModel? Spellcasting { get; init; }
    public List<SpellProgressionReadModel> SpellProgression { get; init; } = [];
}