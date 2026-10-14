namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class Spellcasting
{
    public required string ClassId { get; set; }
    public required string SpellcastingAbility { get; set; }
    public bool IsPreparedCaster { get; set; }
    public Class Class { get; set; } = null!;
}