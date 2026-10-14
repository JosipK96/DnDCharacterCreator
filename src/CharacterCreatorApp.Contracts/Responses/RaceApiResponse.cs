namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// A race as the race step lists it. Edition is "2014" or "2024". The skill choice is from
/// SkillChoiceOptions, or from any skill when that is empty. AbilityChoiceCount abilities of
/// AbilityChoiceOptions (ids, "STR") get +1 each besides the fixed bonuses, a half-elf's two;
/// FeatChoiceCount feats are picked beside the improvements, of FeatChoiceCategory when it is set.
/// </summary>
public record RaceApiResponse(
    string Id,
    string Name,
    string Edition,
    string Source,
    string Size,
    int Speed,
    int SkillChoiceCount,
    List<string> SkillChoiceOptions,
    int LanguageChoiceCount,
    int AbilityChoiceCount,
    List<string> AbilityChoiceOptions,
    int FeatChoiceCount,
    string? FeatChoiceCategory,
    List<AbilityScoreBonusApiResponse> AbilityScoreBonuses,
    List<string> Languages,
    List<TraitApiResponse> Traits,
    List<SubraceApiResponse> Subraces,
    List<ProficiencyApiResponse> FixedProficiencies);

public record AbilityScoreBonusApiResponse(
    string AbilityScore,
    int Bonus);

public record TraitApiResponse(
    string Id,
    string Name,
    string Description);

// Speed is set only where the subrace changes the race's. The language, skill and feat choices
// add to the race's; an ability choice (a variant human's two of six) takes the place of the race's
public record SubraceApiResponse(
    string Id,
    string Name,
    string RaceId,
    string Source,
    int? Speed,
    int LanguageChoiceCount,
    int AbilityChoiceCount,
    List<string> AbilityChoiceOptions,
    int SkillChoiceCount,
    List<string> SkillChoiceOptions,
    int FeatChoiceCount,
    string? FeatChoiceCategory,
    List<AbilityScoreBonusApiResponse> AbilityScoreBonuses,
    List<TraitApiResponse> Traits);
