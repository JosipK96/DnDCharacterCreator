namespace CharacterCreatorApp.Application.ReadModels;

public sealed class FeatureReadModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int Level { get; init; }
    public string? ClassId { get; init; }
    public string? SubclassId { get; init; }
}