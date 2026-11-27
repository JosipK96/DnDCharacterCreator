using CharacterCreatorApp.Contracts.Responses.Races;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Races;

/// <summary>
/// The ability increases a race or subrace has the player pick, as the API sends them: Points
/// spread over Options, from Min to Max on each ability raised. A half-elf's +1 to two abilities
/// besides Charisma, a variant human's to two of six, a Monsters of the Multiverse race's +2 and +1
/// or +1 to three. The API refuses any other spread.
/// </summary>
public sealed record RaceAbilityChoice(List<string> Options, int Points, int Min, int Max)
{
    /// <summary>No increase to pick.</summary>
    public static RaceAbilityChoice None { get; } = new([], 0, 1, 1);

    /// <summary>
    /// The increases the player picks: a subrace's take the place of the race's, as the variant
    /// human's replace the standard human's +1 to everything.
    /// </summary>
    public static RaceAbilityChoice For(RaceApiResponse? race, SubraceApiResponse? subrace) =>
        subrace is { AbilityChoiceCount: > 0 }
            ? Of(subrace)
            : race is { AbilityChoiceCount: > 0 }
                ? new(race.AbilityChoiceOptions, race.AbilityChoiceCount, race.AbilityChoiceMinPerAbility, race.AbilityChoiceMaxPerAbility)
                : None;

    /// <summary>The subrace's own choice, as its option in the list describes it.</summary>
    public static RaceAbilityChoice Of(SubraceApiResponse subrace) =>
        new(subrace.AbilityChoiceOptions, subrace.AbilityChoiceCount, subrace.AbilityChoiceMinPerAbility, subrace.AbilityChoiceMaxPerAbility);

    /// <summary>
    /// "Raise 2 different abilities by 1." as a sentence, or "+1 to 2 abilities" for a subrace's
    /// option in the list.
    /// </summary>
    public string Describe(bool sentence)
    {
        if (Max <= 1)
        {
            return sentence ? $"Raise {Points} different abilities by 1." : $"+1 to {Points} abilities";
        }

        if (Min >= Max)
        {
            var abilities = Points / Max;
            return sentence
                ? $"Raise {abilities} {(abilities == 1 ? "ability" : "different abilities")} by {Max}."
                : $"+{Max} to {abilities} {(abilities == 1 ? "ability" : "abilities")}";
        }

        return sentence
            ? $"Spread {Points} points, at most +{Max} to one ability: pick an ability again to raise it further."
            : $"{Points} points, at most +{Max} to one";
    }

    /// <summary>
    /// Picks the ability once more: each pick raises it by one more point, from the fewest it may
    /// take to the most; past that, or with no points left for another, it is unpicked.
    /// </summary>
    /// <param name="picks">The points picked so far by ability id, changed in place.</param>
    /// <param name="abilityId">The ability picked, by id ("STR").</param>
    public void Raise(Dictionary<string, int> picks, string abilityId)
    {
        var amount = picks.GetValueOrDefault(abilityId);
        var next = amount == 0 ? Min : amount + 1;

        if (next <= Max && next - amount <= Points - picks.Values.Sum())
        {
            picks[abilityId] = next;
            return;
        }

        picks.Remove(abilityId);
    }
}
