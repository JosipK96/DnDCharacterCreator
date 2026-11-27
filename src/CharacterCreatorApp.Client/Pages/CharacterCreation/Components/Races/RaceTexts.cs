using CharacterCreatorApp.Contracts.Responses.Races;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Races;

/// <summary>The words the race step shows about a race or subrace, worked out from what the API sends.</summary>
public static class RaceTexts
{
    /// <summary>
    /// "Choose a Subrace", or for a 2024 species the word its options end in ("Drow Lineage"
    /// makes "Choose a Lineage"), and an ancestry for the dragonborn's dragons, which are just colours.
    /// </summary>
    public static string SubraceHeading(RaceApiResponse race)
    {
        if (race.Edition != "2024")
        {
            return "Choose a Subrace";
        }

        var lastWords = race.Subraces.Select(s => s.Name.Split(' ')[^1]).Distinct().ToList();
        var word = lastWords.Count == 1 && race.Subraces.Count > 1 ? lastWords[0] : "Ancestry";

        return $"Choose {("AEIOU".Contains(word[0]) ? "an" : "a")} {word}";
    }

    /// <summary>What the subrace has the player pick: a variant human's two +1s, a skill and a feat; null for nothing.</summary>
    public static string? SubraceChoices(SubraceApiResponse subrace)
    {
        var choices = new List<string>();

        if (subrace.AbilityChoiceCount > 0)
        {
            choices.Add(RaceAbilityChoice.Of(subrace).Describe(sentence: false));
        }

        if (subrace.SkillChoiceCount > 0)
        {
            choices.Add($"{subrace.SkillChoiceCount} skill(s)");
        }

        if (subrace.FeatChoiceCount > 0)
        {
            choices.Add($"{subrace.FeatChoiceCount} feat(s)");
        }

        return choices.Count == 0 ? null : $"Choose {string.Join(", ", choices)}";
    }

    /// <summary>A 2024 human's Origin feat; the feat is picked with the improvements, where its prerequisites are checked.</summary>
    public static string FeatChoice(int count, string? category) =>
        $"{count} {(category == null ? string.Empty : category + " ")}feat(s) of your choice, picked with the ability score improvements.";

    /// <summary>"+2 CON, +1 STR", or "None".</summary>
    public static string Bonuses(IEnumerable<AbilityScoreBonusApiResponse> bonuses)
    {
        var text = string.Join(", ", bonuses.Select(b => $"+{b.Bonus} {b.AbilityScore.ToUpperInvariant()}"));
        return text.Length == 0 ? "None" : text;
    }
}
