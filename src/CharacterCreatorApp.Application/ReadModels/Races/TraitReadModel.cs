namespace CharacterCreatorApp.Application.ReadModels.Races;

public sealed class TraitReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<string> ProficiencyIds { get; init; } = [];
}