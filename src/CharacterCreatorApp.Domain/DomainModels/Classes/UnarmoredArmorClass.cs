using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Classes;

/// <summary>
/// The armour class a class or subclass gives without armour: Base plus the Dexterity modifier,
/// plus another ability's modifier when there is one. A barbarian's Unarmored Defense is 10 + DEX +
/// CON, a monk's 10 + DEX + WIS, a 2014 draconic sorcerer's Draconic Resilience 13 + DEX. Once
/// armour and shields are modelled it applies only with no armour worn, and with no shield unless
/// AllowsShield (a monk's does not).
/// </summary>
public class UnarmoredArmorClass
{
    public int Base { get; set; } = 10;

    /// <summary>The ability whose modifier is added beside Dexterity's, as a canonical id ("CON"), or null.</summary>
    public string? AbilityId { get; set; }

    public bool AllowsShield { get; set; }

    /// <summary>The armour class for these ability modifiers, keyed by ability in any spelling.</summary>
    public int ArmorClassFor(IReadOnlyDictionary<string, int> modifiers)
    {
        ArgumentNullException.ThrowIfNull(modifiers);

        var byId = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var (key, modifier) in modifiers)
        {
            if (AbilityScore.TryNormalize(key, out var id))
            {
                byId[id] = modifier;
            }
        }

        var armorClass = Base + byId.GetValueOrDefault(AbilityScore.DEX.Id);

        return AbilityId == null ? armorClass : armorClass + byId.GetValueOrDefault(AbilityScore.Normalize(AbilityId));
    }
}
