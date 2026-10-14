namespace CharacterCreatorApp.Contracts.Responses;

public record SpellApiResponse(
    string Id,
    string Name,
    int Level,
    string School,
    string CastingTime,
    string Range,
    string Components,
    string Duration,
    string Description,
    bool IsRitual,
    bool IsConcentration,
    List<string> ClassIds
);