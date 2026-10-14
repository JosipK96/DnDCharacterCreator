namespace CharacterCreatorApp.Application.ReadModels;

public sealed class ProficiencyChoiceReadModel
{
    public required string Type { get; init; }
    public int Count { get; init; }
    public List<ProficiencyReadModel> Options { get; init; } = [];
}