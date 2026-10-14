namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class SubraceAbilityBonus
{
    public int Id { get; set; }
    public required string SubraceId { get; set; }
    public required string AbilityScoreId { get; set; }
    public int Bonus { get; set; }

    public Subrace Subrace { get; set; } = null!;
}