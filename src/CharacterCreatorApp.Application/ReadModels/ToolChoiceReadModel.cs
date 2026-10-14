namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>One tool choice a draft owes: what offers it, how many to pick, and the tools to pick from.</summary>
public sealed class ToolChoiceReadModel
{
    public required string OfferedBy { get; init; }
    public int Count { get; init; }
    public List<ProficiencyReadModel> Options { get; init; } = [];
}
