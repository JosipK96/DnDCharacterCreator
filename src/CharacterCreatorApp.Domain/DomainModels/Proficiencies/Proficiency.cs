using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Domain.DomainModels.Proficiencies;

public class Proficiency
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public List<ClassProficiency> ClassProficiencies { get; set; } = [];
    public List<ProficiencyChoiceOption> ProficiencyChoiceOptions { get; set; } = [];
    public List<RaceProficiency> RaceProficiencies { get; set; } = [];
    public List<SubraceProficiency> SubraceProficiencies { get; set; } = [];
    public List<BackgroundProficiency> BackgroundProficiencies { get; set; } = [];
}