using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Errors;

/// <summary>
/// Errors about a finished character. Shared by creation (which builds one) and management
/// (which reads and deletes one), so it lives here rather than beside either slice.
/// </summary>
public static class CharacterErrors
{
    /// <summary>
    /// Codes for the request-shape rules the validators enforce, kept beside the errors the
    /// handlers return so both use one vocabulary.
    /// </summary>
    public static class Codes
    {
        public const string CharacterIdRequired = "Character.CharacterIdRequired";
    }

    public static Error RaceNotFoundById(string raceId) =>
        Error.NotFound("Character.RaceNotFound", $"Race not found by id {raceId} for character creation");

    public static Error ClassNotFoundById(string classId) =>
        Error.NotFound("Character.ClassNotFound", $"Class not found by id {classId} for character creation");

    public static Error BackgroundNotFoundById(string backgroundId) =>
        Error.NotFound("Character.BackgroundNotFound", $"Background not found by id {backgroundId} for character creation");

    public static Error SubclassNotFoundById(string subclassId) =>
        Error.NotFound("Character.SubclassNotFound", $"Subclass not found by id {subclassId} for character creation");

    public static Error SubraceNotFoundById(string subraceId) =>
        Error.NotFound("Character.SubraceNotFound", $"Subrace not found by id {subraceId} for character creation");

    public static Error NotFoundById(Guid characterId) =>
        Error.NotFound("Character.NotFound", $"Character with id {characterId} was not found.");

    // --- Raised when finalizing a draft ---
    public static Error AbilityScoreMethodRequired() =>
        Error.Validation(
            "Character.AbilityScoreMethodRequired",
            "The ability scores were saved without saying how they were generated. Choose them again in the ability score step.");

    public static Error AbilityScoresDoNotFitMethod(string method) =>
        Error.Validation(
            "Character.AbilityScoresDoNotFitMethod",
            $"The base ability scores are not ones {AbilityScoreMethodName(method)} can produce. Choose them again in the ability score step.");

    internal static string AbilityScoreMethodName(string method) => method switch
    {
        AbilityScoreMethod.StandardArray => "the standard array (15, 14, 13, 12, 10, 8)",
        AbilityScoreMethod.PointBuy => $"point buy ({AbilityScoreMethod.PointBuyBudget} points, scores 8 to 15)",
        _ => "rolling (scores 3 to 18)",
    };

    public static Error SubraceRequired(string raceName, IEnumerable<string> subraceNames) =>
        Error.Validation(
            "Character.SubraceRequired",
            $"A {raceName} must choose one of {string.Join(", ", subraceNames)}.");

    public static Error SubclassRequired(int level, string className, int subclassLevel) =>
        Error.Validation(
            "Character.SubclassRequired",
            $"A level {level} {className} must have a subclass selected. {className} chooses a subclass at level {subclassLevel}.");

    public static Error ProficiencyChoicesIncomplete(string className, int required, int actual) =>
        Error.Validation(
            "Character.ProficiencyChoicesIncomplete",
            $"A {className} must choose exactly {required} proficiencies. The draft has {actual}.");

    public static Error RaceSkillChoicesIncomplete(string raceName, int required, int actual) =>
        Error.Validation(
            "Character.RaceSkillChoicesIncomplete",
            $"A {raceName} must choose exactly {required} skill(s). The draft has {actual}.");

    public static Error RaceAbilityIncreasesIncomplete(string raceName, int count) =>
        Error.Validation(
            "Character.RaceAbilityIncreasesIncomplete",
            $"A {raceName} raises {count} different abilities of the player's choice by 1; pick them with the race.");

    public static Error BackgroundAbilityIncreasesIncomplete(string backgroundName) =>
        Error.Validation(
            "Character.BackgroundAbilityIncreasesIncomplete",
            $"The {backgroundName} background raises three ability scores: pick +2 and +1, or +1 to each.");

    public static Error LanguageChoicesIncomplete(string raceName, string backgroundName, int required, int actual) =>
        Error.Validation(
            "Character.LanguageChoicesIncomplete",
            $"A {raceName} with the {backgroundName} background must choose exactly {required} language(s). The draft has {actual}.");

    public static Error ToolChoicesIncomplete(IEnumerable<string> offeredBy, int picked)
    {
        var names = offeredBy.ToList();
        var description = names.Count == 0
            ? $"The character's race, background and class offer no tools to pick. Got {picked} pick(s)."
            : $"Pick the tools {string.Join(", ", names)} offer(s), one pick each, in that order. Got {picked} pick(s).";

        return Error.Validation("Character.ToolChoicesIncomplete", description);
    }

    public static Error FeatProficiencyPicksIncomplete(IEnumerable<string> featNames, int picked)
    {
        var names = featNames.ToList();
        var description = names.Count == 0
            ? $"No feat of the character's has skills or tools to pick. Got {picked} pick(s)."
            : $"Pick the skills or tools of {string.Join(", ", names)}, one pick each, in that order. Got {picked} pick(s).";

        return Error.Validation("Character.FeatProficiencyPicksIncomplete", description);
    }

    public static Error SizeRequired(string raceName, IEnumerable<string> sizes) =>
        Error.Validation("Character.SizeRequired", $"A {raceName} is {string.Join(" or ", sizes)}; pick one.");

    public static Error BackgroundFeatRequired(string backgroundName, string category) =>
        Error.Validation("Character.BackgroundFeatRequired", $"The {backgroundName} background gives a {category} feat of the player's choice; pick it with the background.");

    public static Error BonusFeatsIncomplete(string raceName, int required, string? category) =>
        Error.Validation(
            "Character.BonusFeatsIncomplete",
            $"A {raceName} takes {required} {(category == null ? string.Empty : category + " ")}feat(s) of its own, first among the feats.");

    public static Error FeatAbilityIncreaseIncomplete(string featName) =>
        Error.Validation(
            "Character.FeatAbilityIncreaseIncomplete",
            $"{featName} raises an ability score of the player's choice; pick it with the feat.");

    public static Error AbilityScoreImprovementsIncomplete(int level, string className, int required, int actual) =>
        Error.Validation(
            "Character.AbilityScoreImprovementsIncomplete",
            $"A level {level} {className} has {required} ability score improvement(s), each spent on ability points or a feat. The draft has {actual}.");
}
