namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// A background as the background step lists it. Edition is "2014" or "2024". A 2024 background
/// raises AbilityScoreOptions (+2 and +1, or +1 to each) and gives a Feat; a 2014 one has a
/// Feature instead, and some 2014 ones a feat as well.
/// </summary>
public record BackgroundApiResponse(
    string Id,
    string Name,
    string Edition,
    string Source,
    List<BackgroundLanguageApiResponse> Languages,
    int LanguageChoiceCount,
    List<BackgroundProficiencyApiResponse> Proficiencies,
    List<string> AbilityScoreOptions,
    BackgroundFeatApiResponse? Feat,
    BackgroundFeatureApiResponse? Feature
);

public record BackgroundLanguageApiResponse(
    string Id,
    string Name
);

public record BackgroundProficiencyApiResponse(
    string Id,
    string Name,
    string Type
);

public record BackgroundFeatApiResponse(
    string Id,
    string Name,
    bool IsRepeatable
);

public record BackgroundFeatureApiResponse(
    string Id,
    string Name,
    string Description
);
