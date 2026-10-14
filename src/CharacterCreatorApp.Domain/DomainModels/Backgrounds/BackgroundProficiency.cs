using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Domain.DomainModels.Backgrounds;

public class BackgroundProficiency
{
    public required string BackgroundId { get; set; }
    public Background Background { get; set; } = null!;
    public required string ProficiencyId { get; set; }
    public Proficiency Proficiency { get; set; } = null!;
}