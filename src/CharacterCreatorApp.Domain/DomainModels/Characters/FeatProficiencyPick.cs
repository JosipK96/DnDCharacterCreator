namespace CharacterCreatorApp.Domain.DomainModels.Characters;

/// <summary>
/// The skills or tools picked for one feat whose choice asks for them (FeatChoice.IsProficiencyPick):
/// Keen Mind's one skill, Skilled's three skills or tools.
/// </summary>
public class FeatProficiencyPick
{
    public required string FeatId { get; set; }

    public List<string> ProficiencyIds { get; set; } = [];
}
