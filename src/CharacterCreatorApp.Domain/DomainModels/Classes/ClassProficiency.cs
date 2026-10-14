using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Domain.DomainModels.Classes;

public class ClassProficiency
{
    public required string ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public required string ProficiencyId { get; set; }
    public Proficiency Proficiency { get; set; } = null!;
}