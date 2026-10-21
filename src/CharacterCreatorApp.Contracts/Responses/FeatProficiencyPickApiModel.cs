namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>The skills or tools picked for one feat that asks for them: Keen Mind's one skill, Skilled's three.</summary>
public class FeatProficiencyPickApiModel
{
    public required string FeatId { get; set; }
    public List<string> ProficiencyIds { get; set; } = [];
}
