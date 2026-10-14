using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Domain.DomainModels.Spells;

public class SpellClass
{
    public int Id { get; set; }
    public required string SpellId { get; set; }
    public required string ClassId { get; set; }
    
    public Spell Spell { get; set; } = null!;
    public Class Class { get; set; } = null!;
}