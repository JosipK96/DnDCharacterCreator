using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Client.UnitTests.Fixtures;

/// <summary>The API's responses as the steps read them, with only what a test is about filled in.</summary>
public static class TestData
{
    public static ClassApiResponse Class(string id, string name, params int[] asiLevels) =>
        new(id, name, "2014", "PHB", "d10", 3, [.. asiLevels], [], null, null, []);

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
        new(id, name, "2014", "PHB", "Medium", 30, 0, [], 0, 0, [], featChoiceCount, featChoiceCategory, [], [], [], [.. subraces], []);

    public static SubraceApiResponse Subrace(string id, string name, string raceId, int featChoiceCount = 0) =>
        new(id, name, raceId, "PHB", null, 0, 0, [], 0, [], featChoiceCount, null, [], []);

    public static BackgroundApiResponse Background(string id, string name, BackgroundFeatApiResponse? feat = null, params BackgroundProficiencyApiResponse[] proficiencies) =>
        new(id, name, "2024", "XPHB", [], 0, [.. proficiencies], [], feat, null, null, null);
}
