namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// The one way the client names an ability: by the canonical id the API uses ("STR"), shown to the
/// player through <see cref="Of"/>. Every step keys its ability scores, increases and improvements
/// by these ids.
/// </summary>
public static class AbilityNames
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["STR"] = "Strength",
        ["DEX"] = "Dexterity",
        ["CON"] = "Constitution",
        ["INT"] = "Intelligence",
        ["WIS"] = "Wisdom",
        ["CHA"] = "Charisma"
    };

    /// <summary>The six ids in the order a character sheet lists them: STR, DEX, CON, INT, WIS, CHA.</summary>
    public static IReadOnlyList<string> Ids { get; } = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    /// <summary>"Strength" for "STR"; an id it does not know comes back as it is.</summary>
    public static string Of(string abilityId) => Names.GetValueOrDefault(abilityId, abilityId);

    /// <summary>
    /// The canonical id for a key in any spelling the API accepts: "STR", "str", "Strength" or
    /// "strength". The improvements step saved full lowercase names before it used the ids, and a
    /// draft or character saved then still holds them. A key it does not know comes back as it is.
    /// </summary>
    public static string IdOf(string key)
    {
        if (Names.ContainsKey(key.ToUpperInvariant()))
        {
            return key.ToUpperInvariant();
        }

        return Names.FirstOrDefault(name => string.Equals(name.Value, key, StringComparison.OrdinalIgnoreCase)).Key ?? key;
    }

    /// <summary>
    /// "Spells (Wisdom)" for a feat's spells cast with the ability picked for them (the 2024 Magic
    /// Initiate's), "Spells" where there was none to pick. The review step and the sheet both use it.
    /// </summary>
    public static string SpellsLabel(string? spellcastingAbilityId) =>
        spellcastingAbilityId is null ? "Spells" : $"Spells ({Of(spellcastingAbilityId)})";
}
