namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>How many Fighting Styles the character picks, and the ones its class offers.</summary>
public record FightingStylesApiResponse(int Owed, List<FeatApiResponse> Options);
