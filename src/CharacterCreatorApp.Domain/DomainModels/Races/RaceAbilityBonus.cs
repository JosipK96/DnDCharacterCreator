namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class RaceAbilityBonus
{
    public int Id { get; set; }
    public required string RaceId { get; set; }
    public required string AbilityScoreId { get; set; }
    public int Bonus { get; set; }

    public Race Race { get; set; } = null!;
}