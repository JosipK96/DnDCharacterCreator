namespace CharacterCreatorApp.Contracts.Responses;

// SpellListClassId is a class whose whole spell list the subclass picks from, such as the
// Wizard's for an Eldritch Knight; null for most subclasses.
public record SubclassApiResponse(
    string Id,
    string Name,
    string ClassId,
    string Source,
    string? SpellListClassId,
    List<SpellProgressionApiResponse> SpellProgression
);
