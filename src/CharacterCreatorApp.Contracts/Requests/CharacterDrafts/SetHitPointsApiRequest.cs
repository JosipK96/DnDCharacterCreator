namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>The hit die's result for the level a level-up reaches: rolled, or the die's fixed value.</summary>
public record SetHitPointsApiRequest(int Result);
