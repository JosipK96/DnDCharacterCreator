namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// The Fighting Style feats for the class's Fighting Style features, as many as
/// GET fighting-styles says are owed, from its options.
/// </summary>
public record ChooseFightingStylesApiRequest(List<string> FeatIds);
