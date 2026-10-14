namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// The character a draft would produce, for the review step before finalizing.
/// <paramref name="BlockingIssue"/> is set when a required choice is still missing.
/// </summary>
public record CharacterPreviewApiResponse(
    string Name,
    int Level,
    string Edition,
    string RaceName,
    string? SubraceName,
    string ClassName,
    string? SubclassName,
    string BackgroundName,
    string Size,
    List<ProficiencyApiResponse> Proficiencies,
    List<LanguageApiResponse> Languages,
    List<FeatureApiResponse> Features,
    List<SpellApiResponse> Spells,
    List<FeatApiResponse> Feats,
    CharacterStatsApiResponse Stats,
    string? BlockingIssue);
