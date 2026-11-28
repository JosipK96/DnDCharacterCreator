using CharacterCreatorApp.Contracts.Responses.Proficiencies;

namespace CharacterCreatorApp.Contracts.Responses.Races;

/// <summary>
/// A race as the race step lists it. Edition is "2014" or "2024". The skill choice is from
/// SkillChoiceOptions, or from any skill when that is empty. AbilityChoiceCount points are
/// spread over AbilityChoiceOptions (ids, "STR") besides the fixed bonuses, from
/// AbilityChoiceMinPerAbility to AbilityChoiceMaxPerAbility on each ability raised: a half-elf's
/// two +1s are 2 points, 1 to 1; a Monsters of the Multiverse race's +2 and +1 or +1 to three are
/// 3 points, 1 to 2; Custom Lineage's +2 to one is 2 points, 2 to 2. FeatChoiceCount feats are
/// picked beside the improvements, of FeatChoiceCategory when it is set. Picks is what the player
/// picks for the race with no subrace, as the server adds it up; each subrace has its own.
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
    List<ProficiencyApiResponse> FixedProficiencies,
    RacePicksApiResponse Picks,
    int AbilityChoiceMaxPerAbility = 1,
    int AbilityChoiceMinPerAbility = 1);

/// <summary>
/// What the player picks for a race with one of its subraces, or with none, as the server's race
/// rules add it up: AbilityChoicePoints spread over AbilityChoiceOptions, from
/// AbilityChoiceMinPerAbility to AbilityChoiceMaxPerAbility on each ability raised (a subrace's
/// choice takes the place of the race's); the skills to pick, from SkillChoiceOptions or any skill
/// when it is empty; the languages to pick; and the feats, of FeatChoiceCategory when it is set.
/// </summary>
public record RacePicksApiResponse(
    List<string> AbilityChoiceOptions,
    int AbilityChoicePoints,
    int AbilityChoiceMinPerAbility,
    int AbilityChoiceMaxPerAbility,
    int SkillChoiceCount,
    List<string> SkillChoiceOptions,
    int LanguageChoiceCount,
    int FeatChoiceCount,
    string? FeatChoiceCategory);

public record AbilityScoreBonusApiResponse(
    string AbilityScore,
    int Bonus);

public record TraitApiResponse(
    string Id,
    string Name,
    string Description);

// Speed is set only where the subrace changes the race's. The language, skill and feat choices
// add to the race's; an ability choice (a variant human's two of six, a SCAG tiefling variant's +2
// to Dexterity or Charisma) takes the place of the race's, read as the race's is. Picks is the race's
// and the subrace's together
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
    List<TraitApiResponse> Traits,
    RacePicksApiResponse Picks,
    int AbilityChoiceMaxPerAbility = 1,
    int AbilityChoiceMinPerAbility = 1);
