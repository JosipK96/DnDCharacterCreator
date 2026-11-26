namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// Whether the draft's character can take a feat. Unmet is what it needs for a feat taken for an
/// improvement ("Strength 13"), UnmetAsRaceFeat what it needs as the race's own feat (a variant
/// human's, which counts no improvement); null means it can. CheckWithDm says the feat also asks
/// for something the app does not check, such as another feat.
/// </summary>
public record FeatEligibilityApiResponse(string FeatId, string? Unmet, string? UnmetAsRaceFeat, bool CheckWithDm);
