namespace CharacterCreatorApp.Domain.ValueObjects;

/// <summary>
/// Ability score increases the player spreads: Points over Options, at most MaxPerAbility on any
/// one. A half-elf's two +1s are 2 points at most 1 each; a 2024 background's +2 and +1, or +1
/// to each, 3 points at most 2 each; a half-feat's +1 is one point.
/// </summary>
public sealed record AbilityIncreaseChoice(IReadOnlyCollection<string> Options, int Points, int MaxPerAbility)
{
    /// <summary>Nothing to spread: only no increases are allowed.</summary>
    public static readonly AbilityIncreaseChoice None = new([], 0, 0);

    /// <summary>
    /// One ability score improvement a class gives when it is spent on ability points: 2 points
    /// over any of the six abilities, at most 2 on one (+2, or +1 to two). Spending it on a feat
    /// instead is not an improvement of this kind.
    /// </summary>
    public static readonly AbilityIncreaseChoice Improvement = new(AbilityScore.All.Select(a => a.Id).ToList(), 2, 2);

    /// <summary>
    /// Whether these increases are the choice's: every ability one of the options, named once
    /// (keys in any spelling, "str" or "Strength"), none above MaxPerAbility, all of Points spent.
    /// </summary>
    public bool Allows(IReadOnlyDictionary<string, int> increases)
    {
        ArgumentNullException.ThrowIfNull(increases);

        var given = increases.Where(kvp => kvp.Value != 0).ToList();
        var abilityIds = new HashSet<string>();

        foreach (var (key, amount) in given)
        {
            if (!AbilityScore.TryNormalize(key, out var abilityId)
                || !Options.Contains(abilityId)
                || !abilityIds.Add(abilityId)
                || amount < 1
                || amount > MaxPerAbility)
            {
                return false;
            }
        }

        return given.Sum(kvp => kvp.Value) == Points;
    }
}
