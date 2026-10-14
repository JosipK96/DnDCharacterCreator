namespace CharacterCreatorApp.Contracts.Responses;

public record CharacterDetailsApiResponse(
    Guid Id,
    string Name,
    int Level,
    string Edition,

    // IDs
    string RaceId,
    string? SubraceId,
    string ClassId,
    string? SubclassId,
    string BackgroundId,

    // Display names
    string RaceName,
    string? SubraceName,
    string ClassName,
    string? SubclassName,
    string BackgroundName,

    // Character data
    Dictionary<string, int> AbilityScores,
    List<ProficiencyApiResponse> Proficiencies,
    List<string> LanguageIds,
    List<LanguageApiResponse> Languages,
    List<FeatureApiResponse> Features,
    List<SpellApiResponse> Spells,
    List<FeatApiResponse> Feats,
    CharacterStatsApiResponse? Stats
);