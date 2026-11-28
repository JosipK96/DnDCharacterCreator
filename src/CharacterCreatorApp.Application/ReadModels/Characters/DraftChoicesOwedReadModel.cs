namespace CharacterCreatorApp.Application.ReadModels.Characters;

/// <summary>
/// What the draft's race, class, background and level owe the player to pick, worked out from the
/// domain model so the wizard's steps count nothing themselves. What depends on a choice not made
/// yet is 0, empty or null.
/// </summary>
public sealed class DraftChoicesOwedReadModel
{
    /// <summary>The improvements (ability points or a feat) the class has given by the draft's level: Class.ImprovementsAt.</summary>
    public int Improvements { get; init; }

    /// <summary>The class levels that give one: Class.AsiLevels.</summary>
    public List<int> ImprovementLevels { get; init; } = [];

    /// <summary>The feats the race and subrace give beside the improvements: a variant human's any one, a 2024 human's Origin one.</summary>
    public int BonusFeats { get; init; }

    /// <summary>The category those feats must be from; null means any feat.</summary>
    public string? BonusFeatCategory { get; init; }

    /// <summary>The feat the background gives: the one picked, where it allows a pick, else its own (Background.FeatIdWith).</summary>
    public string? BackgroundFeatId { get; init; }

    /// <summary>The languages the player picks for the race, subrace and background together.</summary>
    public int Languages { get; init; }

    /// <summary>The languages the race and background give already, by id, as ChooseLanguages matches them.</summary>
    public List<string> KnownLanguageIds { get; init; } = [];

    /// <summary>The class's hit die size (10 for a d10), 0 without a class.</summary>
    public int HitDieSize { get; init; }

    /// <summary>The value a level takes in place of a roll (6 for a d10), 0 without a class.</summary>
    public int HitDieFixedValue { get; init; }
}
