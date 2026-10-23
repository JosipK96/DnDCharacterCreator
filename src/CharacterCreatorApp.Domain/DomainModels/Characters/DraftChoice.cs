namespace CharacterCreatorApp.Domain.DomainModels.Characters;

/// <summary>
/// The wizard steps by name, as CharacterDraft.ChoicesNeeded and LevelUpChoices hold them: the
/// ones an edit could not fill in from what the character kept, and the ones a level-up brings.
/// The client marks each one's step as needing a pick.
/// </summary>
public static class DraftChoice
{
    public const string Race = "race";
    public const string Subclass = "subclass";
    public const string Background = "background";
    public const string Languages = "languages";
    public const string AbilityScores = "ability-scores";
    public const string Proficiencies = "proficiencies";
    public const string Improvements = "improvements";
    public const string Spells = "spells";

    /// <summary>The hit points a level-up adds: the hit die rolled, or its fixed value.</summary>
    public const string HitPoints = "hit-points";

    /// <summary>
    /// What a character keeps no choice for: everything but the name, level, books, class and
    /// subclass, which it stores as they were picked.
    /// </summary>
    public static IReadOnlyList<string> NotKeptByCharacter { get; } =
        [Race, Background, Languages, AbilityScores, Proficiencies, Improvements, Spells];
}
