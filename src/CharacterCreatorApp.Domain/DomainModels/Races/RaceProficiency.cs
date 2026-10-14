using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class RaceProficiency
{
    public required string RaceId { get; set; }
    public Race Race { get; set; } = null!;
    public required string ProficiencyId { get; set; }
    public Proficiency Proficiency { get; set; } = null!;
}