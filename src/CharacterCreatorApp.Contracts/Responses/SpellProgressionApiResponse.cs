namespace CharacterCreatorApp.Contracts.Responses;

public record SpellProgressionApiResponse(
    int CharacterLevel,
    int SpellsKnown,
    int CantripsKnown,
    int MaxSpellLevel,
    string? SpellcastingAbility);