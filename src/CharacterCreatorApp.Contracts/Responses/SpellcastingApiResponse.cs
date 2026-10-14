namespace CharacterCreatorApp.Contracts.Responses;

public record SpellcastingApiResponse(
    string SpellcastingAbility,
    bool IsPreparedCaster
);