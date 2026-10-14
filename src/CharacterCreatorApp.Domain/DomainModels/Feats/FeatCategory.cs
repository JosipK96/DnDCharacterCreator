using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Feats;

/// <summary>The feat categories with rules of their own (Feat.Category).</summary>
public static class FeatCategory
{
    public const string FightingStyle = "Fighting Style";

    /// <summary>
    /// Whether an improvement may be spent on a feat of this edition and category. A 2014
    /// Fighting Style is a class option the app keeps as a feat, which no improvement buys; a 2024
    /// one is a feat a character with the Fighting Style feature may take for one.
    /// </summary>
    public static bool CanBeTakenForImprovement(string edition, string? category) =>
        !(category == FightingStyle && edition == RulesEdition.Edition2014);
}
