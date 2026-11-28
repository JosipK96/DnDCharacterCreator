using CharacterCreatorApp.Contracts.Responses.Backgrounds;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Classes;
using CharacterCreatorApp.Contracts.Responses.Feats;
using CharacterCreatorApp.Contracts.Responses.Proficiencies;
using CharacterCreatorApp.Contracts.Responses.Races;
using CharacterCreatorApp.Contracts.Responses.Spells;

namespace CharacterCreatorApp.Client.UnitTests.Fixtures;

/// <summary>The API's responses as the steps read them, with only what a test is about filled in.</summary>
public static class TestData
{
    public static ClassApiResponse Class(string id, string name, params int[] asiLevels) =>
        new(id, name, "2014", "PHB", "d10", 3, [.. asiLevels], [], null, null, [], ["STR", "CON"]);

    public static FeatApiResponse Feat(
        string id,
        string name,
        FeatAbilityIncreaseApiResponse? increase = null,
        FeatChoiceApiResponse? choice = null,
        string? category = null) =>
        new(id, name, "2014", "PHB", category, $"{name}'s description.", [], [], increase, true, choice);

    public static ProficiencyApiResponse Skill(string id, string name) => new(id, name, "skill");

    public static SpellApiResponse Spell(string id, string name, int level, string school = "Evocation") =>
        new(id, name, "PHB", level, school, "1 action", "60 feet", "V, S", "Instantaneous", $"{name}'s description.", false, false, []);

    public static RaceApiResponse Race(string id, string name, int featChoiceCount = 0, string? featChoiceCategory = null, params SubraceApiResponse[] subraces) =>
        new(id, name, "2014", "PHB", "Medium", 30, 0, [], 0, 0, [], featChoiceCount, featChoiceCategory, [], [], [], [.. subraces], [],
            Picks(feats: featChoiceCount, featCategory: featChoiceCategory));

    public static SubraceApiResponse Subrace(string id, string name, string raceId, int featChoiceCount = 0) =>
        new(id, name, raceId, "PHB", null, 0, 0, [], 0, [], featChoiceCount, null, [], [], Picks(feats: featChoiceCount));

    /// <summary>What the API says the player picks for a race with a subrace, or with none; nothing unless a test says.</summary>
    public static RacePicksApiResponse Picks(
        int abilityPoints = 0,
        List<string>? abilityOptions = null,
        int min = 1,
        int max = 1,
        int skills = 0,
        List<string>? skillOptions = null,
        int languages = 0,
        int feats = 0,
        string? featCategory = null) =>
        new(abilityOptions ?? [], abilityPoints, min, max, skills, skillOptions ?? [], languages, feats, featCategory);

    public static BackgroundApiResponse Background(string id, string name, BackgroundFeatApiResponse? feat = null, params BackgroundProficiencyApiResponse[] proficiencies) =>
        new(id, name, "2024", "XPHB", [], 0, [.. proficiencies], [], feat, null, null, null, null);

    /// <summary>What the API says a draft owes; nothing unless a test says.</summary>
    public static DraftChoicesOwedApiResponse Owed(
        int improvements = 0,
        List<int>? improvementLevels = null,
        int bonusFeats = 0,
        string? bonusFeatCategory = null,
        string? backgroundFeatId = null,
        int languages = 0,
        List<string>? knownLanguageIds = null,
        int hitDieSize = 0,
        int hitDieFixedValue = 0) =>
        new(improvements, improvementLevels ?? [], bonusFeats, bonusFeatCategory, backgroundFeatId, languages, knownLanguageIds ?? [], hitDieSize, hitDieFixedValue);
}
