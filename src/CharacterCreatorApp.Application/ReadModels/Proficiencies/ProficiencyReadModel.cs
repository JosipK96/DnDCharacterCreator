namespace CharacterCreatorApp.Application.ReadModels.Proficiencies;

public sealed class ProficiencyReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
}