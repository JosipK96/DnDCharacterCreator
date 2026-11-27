using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.ReadModels.Races;

public sealed class AbilityScoreBonusReadModel
{
    public AbilityScore AbilityScore { get; init; } = null!;
    public int Bonus { get; init; }
}