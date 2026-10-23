namespace CharacterCreatorApp.Domain.DomainModels.Characters;

/// <summary>
/// The wizard steps an edit could not fill in from what the character kept, by name, as
/// CharacterDraft.ChoicesNeeded holds them. The client marks each one's step as needing a pick.
/// </summary>
public static class DraftChoice
{
    public const string Race = "race";
    public const string Background = "background";
    public const string Languages = "languages";
    public const string AbilityScores = "ability-scores";
    public const string Proficiencies = "proficiencies";
    public const string Improvements = "improvements";
    public const string Spells = "spells";

    /// <summary>
    /// What a character keeps no choice for: everything but the name, level, books, class and
    /// subclass, which it stores as they were picked.
    /// </summary>
    public static IReadOnlyList<string> NotKeptByCharacter { get; } =
        [Race, Background, Languages, AbilityScores, Proficiencies, Improvements, Spells];
}
