using CharacterCreatorApp.Contracts.Responses.Races;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Races;

/// <summary>
/// The skills a race and its subrace have the player pick, together: a half-elf's two of any, a
/// variant human's one, a 2024 elf's Keen Senses one of three.
/// </summary>
public static class RaceSkillChoice
{
    /// <summary>How many skills to pick for the race and subrace together; a variant human's comes with the subrace.</summary>
    public static int CountFor(RaceApiResponse? race, SubraceApiResponse? subrace) =>
        (race?.SkillChoiceCount ?? 0) + (subrace?.SkillChoiceCount ?? 0);

    /// <summary>The skill ids to pick from; empty means any skill, as when either pick has no list.</summary>
    public static List<string> OptionsFor(RaceApiResponse? race, SubraceApiResponse? subrace)
    {
        var anyFromRace = race is { SkillChoiceCount: > 0, SkillChoiceOptions.Count: 0 };
        var anyFromSubrace = subrace is { SkillChoiceCount: > 0, SkillChoiceOptions.Count: 0 };

        return anyFromRace || anyFromSubrace ? [] : [.. race?.SkillChoiceOptions ?? [], .. subrace?.SkillChoiceOptions ?? []];
    }
}
