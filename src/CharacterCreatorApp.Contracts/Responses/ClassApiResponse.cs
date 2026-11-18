namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// A class as the class step lists it. SavingThrowAbilityIds are the ability ids ("STR") whose
/// saving throws the class is proficient in, which the step's key panel shows.
/// </summary>
public record ClassApiResponse(
    string Id,
    string Name,
    string Edition,
    string Source,
    string HitDie,
    int SubclassLevel,
    List<int> AsiLevels,
    List<ProficiencyApiResponse> ClassProficiencies,
    ProficiencyChoiceApiResponse? ProficiencyChoice,
    SpellcastingApiResponse? Spellcasting,
    List<SpellProgressionApiResponse> SpellProgression,
    List<string> SavingThrowAbilityIds);
