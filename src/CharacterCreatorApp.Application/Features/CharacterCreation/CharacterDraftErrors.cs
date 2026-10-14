using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation;

/// <summary>
/// Every error the character creation slices can return. Codes are namespaced
/// "CharacterDraft.&lt;Reason&gt;" and live here rather than as strings in handlers, so a code is
/// defined once and validators can take the same one through .WithErrorCode(...).
/// </summary>
public static class CharacterDraftErrors
{
    /// <summary>
    /// Codes for the request-shape rules the FluentValidation validators enforce. They live here
    /// with the rest so a validator and a handler never invent two codes for the same thing;
    /// every rule passes one to .WithErrorCode(...) instead of falling back to FluentValidation's
    /// validator name ("NotEmptyValidator").
    /// </summary>
    public static class Codes
    {
        public const string DraftIdRequired = "CharacterDraft.DraftIdRequired";
        public const string NameRequired = "CharacterDraft.NameRequired";
        public const string NameTooLong = "CharacterDraft.NameTooLong";
        public const string InvalidLevel = "CharacterDraft.InvalidLevel";
        public const string InvalidEdition = "CharacterDraft.InvalidEdition";
        public const string RaceIdRequired = "CharacterDraft.RaceIdRequired";
        public const string ClassIdRequired = "CharacterDraft.ClassIdRequired";
        public const string SubclassIdRequired = "CharacterDraft.SubclassIdRequired";
        public const string BackgroundIdRequired = "CharacterDraft.BackgroundIdRequired";
        public const string LanguageIdsRequired = "CharacterDraft.LanguageIdsRequired";
        public const string BlankLanguageId = "CharacterDraft.BlankLanguageId";
        public const string ProficiencyIdsRequired = "CharacterDraft.ProficiencyIdsRequired";
        public const string BlankProficiencyId = "CharacterDraft.BlankProficiencyId";
        public const string SpellIdsRequired = "CharacterDraft.SpellIdsRequired";
        public const string ImprovementsRequired = "CharacterDraft.ImprovementsRequired";
        public const string FeatAbilityIncreasesMismatch = "CharacterDraft.FeatAbilityIncreasesMismatch";
        public const string InvalidAbilityScoreCount = "CharacterDraft.InvalidAbilityScoreCount";
        public const string InvalidAbilityScoreKeys = "CharacterDraft.InvalidAbilityScoreKeys";
        public const string InvalidAbilityScoreMethod = "CharacterDraft.InvalidAbilityScoreMethod";
        public const string AbilityScoresDoNotFitMethod = "CharacterDraft.AbilityScoresDoNotFitMethod";
    }

    public static Error NotFoundById(Guid draftId) =>
        Error.NotFound("CharacterDraft.NotFound", $"Character draft not found by id {draftId}");

    public static Error NoIncompleteDraft() =>
        Error.NotFound("CharacterDraft.NoIncompleteDraft", "There is no unfinished character draft to resume");

    public static Error Invalid() =>
        Error.Validation("CharacterDraft.Invalid", "Character draft is incomplete");

    // --- Steps that depend on an earlier choice ---
    public static Error RaceRequired(string step) =>
        Error.Validation("CharacterDraft.RaceRequired", $"Must choose a race before {step}");

    public static Error BackgroundRequired(string step) =>
        Error.Validation("CharacterDraft.BackgroundRequired", $"Must choose a background before {step}");

    public static Error ClassRequired(string step) =>
        Error.Validation("CharacterDraft.ClassRequired", $"Must choose a class before {step}");

    public static Error LevelRequired(string step) =>
        Error.Validation("CharacterDraft.LevelRequired", $"Must choose a level before {step}");

    public static Error ClassAndLevelRequired() =>
        Error.Validation("CharacterDraft.ClassAndLevelRequired", "Must choose class and level before setting ability score improvements");

    /// <summary>A choice from the other rules edition than the one the character is built with.</summary>
    public static Error EditionMismatch(string what, string name, string itsEdition, string draftEdition) =>
        Error.Validation("CharacterDraft.EditionMismatch", $"The {what} {name} follows the {itsEdition} rules, but this character is built with the {draftEdition} rules.");

    // --- Race ability increases ---
    public static Error InvalidRaceAbilityIncreases(string raceName, int count, IReadOnlyCollection<string> abilityNames)
    {
        var description = count == 0
            ? $"{raceName} has no ability increases to pick."
            : $"{raceName} raises {count} different abilities by 1, picked from {string.Join(", ", abilityNames)}.";

        return Error.Validation("CharacterDraft.InvalidRaceAbilityIncreases", description);
    }

    // --- Background ability increases ---
    public static Error InvalidBackgroundAbilityIncreases(string backgroundName, IReadOnlyCollection<string> abilityNames)
    {
        var description = abilityNames.Count == 0
            ? $"{backgroundName} raises no ability scores; the race does."
            : $"{backgroundName} raises {string.Join(", ", abilityNames)}: +2 to one and +1 to another, or +1 to each.";

        return Error.Validation("CharacterDraft.InvalidBackgroundAbilityIncreases", description);
    }

    // --- Languages ---
    public static Error InvalidLanguageCount(int required, int selected) =>
        Error.Validation("CharacterDraft.InvalidLanguageCount", $"Must choose exactly {required} language(s). You selected {selected}.");

    public static Error DuplicateLanguages() =>
        Error.Validation("CharacterDraft.DuplicateLanguages", "Cannot select the same language multiple times");

    public static Error InvalidLanguageIds(IEnumerable<string> ids) =>
        Error.Validation("CharacterDraft.InvalidLanguageIds", $"These languages can't be chosen: {string.Join(", ", ids)}. Only the standard and exotic Player's Handbook languages can.");

    public static Error LanguagesAlreadyKnown(IEnumerable<string> names) =>
        Error.Validation("CharacterDraft.LanguagesAlreadyKnown", $"Your race or background already gives you {string.Join(", ", names)}; choose other languages.");

    // --- Class proficiency choices ---
    public static Error InvalidProficiencyCount(int required, string type, string classId, int selected) =>
        Error.Validation("CharacterDraft.InvalidProficiencyCount", $"Must choose exactly {required} {type} proficiencies for {classId}. You selected {selected}.");

    public static Error InvalidProficiencySelection(IEnumerable<string> invalid, IEnumerable<string> available) =>
        Error.Validation("CharacterDraft.InvalidProficiencySelection", $"Invalid proficiency selections: {string.Join(", ", invalid)}. Available options: {string.Join(", ", available)}");

    public static Error DuplicateProficiencies() =>
        Error.Validation("CharacterDraft.DuplicateProficiencies", "Cannot select the same proficiency multiple times");

    // --- Race skill choices ---
    public static Error InvalidRaceSkillCount(int required, string raceName, int selected) =>
        Error.Validation("CharacterDraft.InvalidRaceSkillCount", $"Must choose exactly {required} skill(s) for {raceName}. You selected {selected}.");

    public static Error DuplicateRaceSkills() =>
        Error.Validation("CharacterDraft.DuplicateRaceSkills", "Cannot select the same skill proficiency multiple times");

    public static Error InvalidRaceSkillType(IEnumerable<string> ids) =>
        Error.Validation("CharacterDraft.InvalidRaceSkillType", $"Selected proficiencies must be skills: {string.Join(", ", ids)}");

    public static Error RaceSkillsNotOffered(string raceName, IEnumerable<string> ids, IEnumerable<string> options) =>
        Error.Validation("CharacterDraft.RaceSkillsNotOffered", $"{raceName} does not offer {string.Join(", ", ids)}; choose from {string.Join(", ", options)}.");

    // --- Ability score improvements and feats ---
    public static Error InvalidAsiCount(int level, string classId, int expected, int actual) =>
        Error.Validation("CharacterDraft.InvalidAsiCount", $"A level {level} {classId} has {expected} ability score improvement(s), each spent on ability points or a feat. Got {actual}.");

    public static Error EmptyAsi() =>
        Error.Validation("CharacterDraft.EmptyAsi", "Each ASI must specify at least one ability score");

    public static Error InvalidAsiPoints(int total) =>
        Error.Validation("CharacterDraft.InvalidAsiPoints", $"Each ASI must distribute exactly 2 points. Got {total}.");

    public static Error InvalidAsiAbility(IEnumerable<string> abilities) =>
        Error.Validation("CharacterDraft.InvalidAsiAbility", $"Invalid ability score(s): {string.Join(", ", abilities)}");

    public static Error InvalidAsiValue() =>
        Error.Validation("CharacterDraft.InvalidAsiValue", "Each ability score in an ASI must receive 1 or 2 points");

    public static Error InvalidFeatIds(IEnumerable<string> ids) =>
        Error.Validation("CharacterDraft.InvalidFeatIds", $"Unknown feat ids: {string.Join(", ", ids)}");

    public static Error DuplicateFeats(IEnumerable<string> names) =>
        Error.Validation("CharacterDraft.DuplicateFeats", $"These feats can only be taken once: {string.Join(", ", names)}");

    /// <summary>A picked proficiency the character has already, from the race, background, class or the other pick.</summary>
    public static Error ProficiencyAlreadyGiven(string proficiencyId, string source) =>
        Error.Validation("CharacterDraft.ProficiencyAlreadyGiven", $"{proficiencyId} comes with {source} already; pick another in its place.");

    /// <summary>A feat whose requirement the character falls short of, worded as FeatPrerequisites.FindUnmet words it.</summary>
    public static Error FeatPrerequisiteNotMet(string featName, string unmetRequirement) =>
        Error.Validation("CharacterDraft.FeatPrerequisiteNotMet", $"{featName} requires {unmetRequirement}.");

    public static Error BonusFeatsMissing(string raceName, int required, int actual) =>
        Error.Validation("CharacterDraft.BonusFeatsMissing", $"{raceName} gives {required} feat(s) besides the improvements, listed first among the feats. Got {actual} feat(s).");

    public static Error BonusFeatNotOfCategory(string raceName, string featName, string category) =>
        Error.Validation("CharacterDraft.BonusFeatNotOfCategory", $"The feat {raceName} gives must be of the {category} category; {featName} is not.");

    public static Error InvalidFeatAbilityIncrease(string featName, int points, IReadOnlyCollection<string> abilityNames)
    {
        var description = abilityNames.Count switch
        {
            0 => $"{featName} raises no ability score.",
            1 => $"{featName} raises {abilityNames.First()} by {points}; there is nothing to pick.",
            _ when points == 1 => $"{featName} raises one of {string.Join(", ", abilityNames)} by 1.",
            _ => $"{featName} gives {points} points to spread over {string.Join(", ", abilityNames)}.",
        };

        return Error.Validation("CharacterDraft.InvalidFeatAbilityIncrease", description);
    }

    // --- Subrace and subclass ---
    public static Error NoSubracesAvailable(string raceName) =>
        Error.Validation("CharacterDraft.NoSubracesAvailable", $"Race '{raceName}' does not have any subraces available");

    public static Error InvalidSubrace(string subraceId, string raceId, IEnumerable<string> availableNames) =>
        Error.Validation("CharacterDraft.InvalidSubrace", $"Subrace '{subraceId}' is not valid for race '{raceId}'. Available subraces: {string.Join(", ", availableNames)}");

    public static Error InvalidSubclass(string subclassId, string classId, IEnumerable<string> availableNames) =>
        Error.Validation("CharacterDraft.InvalidSubclass", $"Subclass '{subclassId}' is not valid for class '{classId}'. Available subclasses: {string.Join(", ", availableNames)}");

    public static Error SubclassLevelRequirement(string className, int subclassLevel, int currentLevel) =>
        Error.Validation("CharacterDraft.SubclassLevelRequirement", $"A {className} chooses a subclass at level {subclassLevel} (current level: {currentLevel})");
}
