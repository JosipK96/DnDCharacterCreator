using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.ReadModels;

public sealed class AbilityScoreBonusReadModel
{
    public AbilityScore AbilityScore { get; init; } = null!;
    public int Bonus { get; init; }
}