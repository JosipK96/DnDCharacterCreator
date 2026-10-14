namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class SubraceTrait
{
    public required string Id { get; set; }
    public required string SubraceId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    public Subrace Subrace { get; set; } = null!;
}