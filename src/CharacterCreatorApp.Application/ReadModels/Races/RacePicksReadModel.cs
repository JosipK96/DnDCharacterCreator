namespace CharacterCreatorApp.Application.ReadModels.Races;

/// <summary>
/// What the player picks for a race with one of its subraces, or with none, as the Race entity's
/// *With members work it out, so the race step adds nothing up itself: the subrace's picks come on
/// top of the race's, and a subrace's ability choice takes the place of the race's.
/// </summary>
public sealed class RacePicksReadModel
{
    /// <summary>The abilities the increases are spread over, as canonical ids.</summary>
    public List<string> AbilityChoiceOptions { get; init; } = [];

    /// <summary>The points spread over them; 0 when there is nothing to pick.</summary>
    public int AbilityChoicePoints { get; init; }

    /// <summary>The fewest points an ability raised takes: 2 for Custom Lineage's +2 to one.</summary>
    public int AbilityChoiceMinPerAbility { get; init; }

    /// <summary>The most points one ability takes: 2 for a Monsters of the Multiverse race's +2 and +1.</summary>
    public int AbilityChoiceMaxPerAbility { get; init; }

    public int SkillChoiceCount { get; init; }

    /// <summary>The skills those picks are from; empty means any skill.</summary>
    public List<string> SkillChoiceOptions { get; init; } = [];

    public int LanguageChoiceCount { get; init; }

    /// <summary>The feats the race gives to pick (a 2024 human's Origin feat); a null category means any feat.</summary>
    public int FeatChoiceCount { get; init; }

    public string? FeatChoiceCategory { get; init; }
}
