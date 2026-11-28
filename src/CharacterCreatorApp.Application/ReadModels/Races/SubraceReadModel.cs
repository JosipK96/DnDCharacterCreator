namespace CharacterCreatorApp.Application.ReadModels.Races;

public sealed class SubraceReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string RaceId { get; init; }

    /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The subrace's own speed where it changes the race's (a wood elf's 35), else null.</summary>
    public int? Speed { get; init; }

    /// <summary>Languages picked on top of the race's: a high elf's one.</summary>
    public int LanguageChoiceCount { get; init; }

    /// <summary>Points spread in place of the race's choice: a variant human's two +1s of six.</summary>
    public int AbilityChoiceCount { get; init; }

    public List<string> AbilityChoiceOptions { get; init; } = [];

    /// <summary>The most points one ability takes: 2 for a SCAG tiefling variant's +2.</summary>
    public int AbilityChoiceMaxPerAbility { get; init; } = 1;

    /// <summary>The fewest points an ability raised takes: 2 for a SCAG tiefling variant's +2.</summary>
    public int AbilityChoiceMinPerAbility { get; init; } = 1;

    /// <summary>Skills picked on top of the race's: a variant human's one. Empty options mean any skill.</summary>
    public int SkillChoiceCount { get; init; }

    public List<string> SkillChoiceOptions { get; init; } = [];

    /// <summary>Feats picked on top of the race's: a variant human's one; a null category means any feat.</summary>
    public int FeatChoiceCount { get; init; }

    public string? FeatChoiceCategory { get; init; }

    public List<AbilityScoreBonusReadModel> AbilityScoreBonuses { get; init; } = [];
    public List<TraitReadModel> Traits { get; init; } = [];

    /// <summary>What the player picks for the race with this subrace, the race's and the subrace's together.</summary>
    public required RacePicksReadModel Picks { get; init; }
}
