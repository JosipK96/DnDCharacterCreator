namespace CharacterCreatorApp.Contracts.Responses.Characters;

/// <summary>
/// What the draft owes the player to pick, worked out by the server from its race, class,
/// background and level: the improvements by the draft's level and the class levels that give
/// them, the race's bonus feats and their category (null for any), the feat the background gives,
/// the languages to pick and those already known by id, and the class's hit die size and fixed
/// value. What depends on a choice not made yet is 0, empty or null.
/// </summary>
public record DraftChoicesOwedApiResponse(
    int Improvements,
    List<int> ImprovementLevels,
    int BonusFeats,
    string? BonusFeatCategory,
    string? BackgroundFeatId,
    int Languages,
    List<string> KnownLanguageIds,
    int HitDieSize,
    int HitDieFixedValue);
