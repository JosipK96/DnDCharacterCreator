namespace CharacterCreatorApp.Application.ReadModels;

public sealed class RaceReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024.</summary>
    public required string Edition { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
    public string Source { get; init; } = string.Empty;

    public required string Size { get; init; }
    public int Speed { get; init; }
    public List<AbilityScoreBonusReadModel> AbilityScoreBonuses { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public List<TraitReadModel> Traits { get; init; } = [];
    public List<SubraceReadModel> Subraces { get; init; } = [];
    public List<ProficiencyReadModel> FixedProficiencies { get; init; } = [];
    public int SkillChoiceCount { get; init; }

    /// <summary>The skills the race's pick is from; empty means any skill.</summary>
    public List<string> SkillChoiceOptions { get; init; } = [];

    public int LanguageChoiceCount { get; init; }

    /// <summary>Abilities the player raises by 1 besides the fixed bonuses (a half-elf's two), from AbilityChoiceOptions.</summary>
    public int AbilityChoiceCount { get; init; }

    public List<string> AbilityChoiceOptions { get; init; } = [];

    /// <summary>Feats the race gives to pick (a 2024 human's Origin feat); a null category means any feat.</summary>
    public int FeatChoiceCount { get; init; }

    public string? FeatChoiceCategory { get; init; }
}
