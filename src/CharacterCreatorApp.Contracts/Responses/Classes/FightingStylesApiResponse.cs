using CharacterCreatorApp.Contracts.Responses.Feats;

namespace CharacterCreatorApp.Contracts.Responses.Classes;

/// <summary>How many Fighting Styles the character picks, and the ones its class offers.</summary>
public record FightingStylesApiResponse(int Owed, List<FeatApiResponse> Options);
