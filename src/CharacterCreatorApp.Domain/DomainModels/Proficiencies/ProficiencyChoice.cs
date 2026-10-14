using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Domain.DomainModels.Proficiencies;

public class ProficiencyChoice
{
    public int Id { get; set; }
    public required string ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public required string Type { get; set; }
    public int Count { get; set; }
    public List<ProficiencyChoiceOption> Options { get; set; } = [];
}