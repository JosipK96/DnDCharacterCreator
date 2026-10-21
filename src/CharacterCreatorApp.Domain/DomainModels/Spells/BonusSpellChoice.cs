namespace CharacterCreatorApp.Domain.DomainModels.Spells;

/// <summary>
/// Spells a feat or race lets the player pick from a class's spell list, beside the class's own:
/// Magic Initiate's two cantrips and one 1st-level spell from one of several lists, a 2014 high
/// elf's wizard cantrip. With one class the list is fixed.
/// </summary>
public class BonusSpellChoice
{
    /// <summary>The class ids whose lists the player picks one of ("cleric-2024").</summary>
    public List<string> ClassIds { get; set; } = [];

    public int Cantrips { get; set; }

    public int FirstLevelSpells { get; set; }

    /// <summary>
    /// The spellcasting abilities the player picks one of for these spells, by canonical id: the
    /// 2024 Magic Initiate's INT, WIS or CHA. Empty when there is no pick.
    /// </summary>
    public List<string> AbilityOptions { get; set; } = [];
}
