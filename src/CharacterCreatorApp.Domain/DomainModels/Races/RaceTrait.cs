namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class RaceTrait
{
    public required string Id { get; set; }
    public required string RaceId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    public Race Race { get; set; } = null!;
}