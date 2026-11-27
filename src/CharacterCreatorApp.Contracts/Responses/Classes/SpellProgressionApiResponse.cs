namespace CharacterCreatorApp.Contracts.Responses.Classes;

public record SpellProgressionApiResponse(
    int CharacterLevel,
    int SpellsKnown,
    int CantripsKnown,
    int MaxSpellLevel,
    string? SpellcastingAbility);