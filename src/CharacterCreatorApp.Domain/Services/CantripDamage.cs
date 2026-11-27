namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// How a cantrip's damage grows: both rule books raise it at character levels 5, 11 and 17, so a
/// cantrip's dice are kept for each of those tiers (Spell.CantripDamageDice).
/// </summary>
public static class CantripDamage
{
    /// <summary>The character levels a cantrip's damage changes at, the first being where it starts.</summary>
    public static readonly IReadOnlyList<int> TierLevels = [1, 5, 11, 17];

    /// <summary>The dice at the character's level ("2d10" at level 5 for Fire Bolt), or null when there are none for every tier.</summary>
    public static string? DiceAt(IReadOnlyList<string> diceByTier, int level)
    {
        ArgumentNullException.ThrowIfNull(diceByTier);

        if (diceByTier.Count != TierLevels.Count)
        {
            return null;
        }

        var tier = TierLevels.Count(tierLevel => level >= tierLevel) - 1;

        return diceByTier[Math.Max(tier, 0)];
    }

    /// <summary>
    /// Whether a spell belongs in the sheet's attacks table: a cantrip that makes a spell attack or
    /// calls for a saving throw and deals damage with dice for every tier. Light (a save, no damage)
    /// and levelled spells, whose damage depends on the slot, are not.
    /// </summary>
    public static bool IsAttackCantrip(int spellLevel, string? attackType, string? saveAbilityId, string? damageType, IReadOnlyCollection<string> diceByTier)
    {
        ArgumentNullException.ThrowIfNull(diceByTier);

        return spellLevel == 0
            && (attackType != null || saveAbilityId != null)
            && damageType != null
            && diceByTier.Count == TierLevels.Count;
    }
}
