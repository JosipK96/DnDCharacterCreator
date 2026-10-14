using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class SubraceProficiency
{
    public required string SubraceId { get; set; }
    public Subrace Subrace { get; set; } = null!;
    public required string ProficiencyId { get; set; }
    public Proficiency Proficiency { get; set; } = null!;
}