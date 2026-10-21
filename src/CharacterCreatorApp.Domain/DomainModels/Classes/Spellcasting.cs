namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class Spellcasting
{
    public required string ClassId { get; set; }
    public required string SpellcastingAbility { get; set; }
    public bool IsPreparedCaster { get; set; }

    /// <summary>
    /// A 2014 class that prepares its spellcasting modifier plus its level in spells divides the
    /// level by this: 1 for a cleric, druid or wizard, 2 for a paladin or artificer. Null when the
    /// class has no such rule, every 2024 class among them: their spell table gives the count.
    /// </summary>
    public int? PreparedSpellsLevelDivisor { get; set; }

    public Class Class { get; set; } = null!;
}
