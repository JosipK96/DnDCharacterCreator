namespace CharacterCreatorApp.Contracts.Responses.Classes;

public record SpellcastingApiResponse(
    string SpellcastingAbility,
    bool IsPreparedCaster
);