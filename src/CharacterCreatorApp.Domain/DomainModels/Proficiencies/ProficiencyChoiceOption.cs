namespace CharacterCreatorApp.Domain.DomainModels.Proficiencies;

public class ProficiencyChoiceOption
{
    public int ProficiencyChoiceId { get; set; }
    public ProficiencyChoice ProficiencyChoice { get; set; } = null!;
    public required string ProficiencyId { get; set; }
    public Proficiency Proficiency { get; set; } = null!;
}