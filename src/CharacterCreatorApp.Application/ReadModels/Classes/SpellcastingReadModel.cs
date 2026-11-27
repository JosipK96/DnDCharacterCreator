namespace CharacterCreatorApp.Application.ReadModels.Classes;

public sealed class SpellcastingReadModel
{
    public required string SpellcastingAbility { get; init; }
    public bool IsPreparedCaster { get; init; }
}