namespace CharacterCreatorApp.Client.Pages.CharacterCreation;

/// <summary>
/// The abilities' names by the ids the API uses ("STR"), for the steps that offer increases:
/// a 2024 background's, a half-elf's or variant human's, a feat's.
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

    /// <summary>"Strength" for "STR"; an id it does not know comes back as it is.</summary>
    public static string Of(string abilityId) => Names.GetValueOrDefault(abilityId, abilityId);

    /// <summary>
    /// "Spells (Wisdom)" for a feat's spells cast with the ability picked for them (the 2024 Magic
    /// Initiate's), "Spells" where there was none to pick. The review step and the sheet both use it.
    /// </summary>
    public static string SpellsLabel(string? spellcastingAbilityId) =>
        spellcastingAbilityId is null ? "Spells" : $"Spells ({Of(spellcastingAbilityId)})";
}
