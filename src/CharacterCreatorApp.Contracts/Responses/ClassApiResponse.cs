namespace CharacterCreatorApp.Contracts.Responses;

public record ClassApiResponse(
    string Id,
    string Name,
    string Edition,
    string HitDie,
    int SubclassLevel,
    List<int> AsiLevels,
    List<ProficiencyApiResponse> ClassProficiencies,
    ProficiencyChoiceApiResponse? ProficiencyChoice,
    SpellcastingApiResponse? Spellcasting,
    List<SpellProgressionApiResponse> SpellProgression);