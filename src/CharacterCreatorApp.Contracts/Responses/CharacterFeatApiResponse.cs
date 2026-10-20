namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// One feat on the character sheet, with the spells picked for it (Magic Initiate's), empty when
/// the feat has no spell choice. A feat taken twice is two of these, each with its own spells.
/// </summary>
public record CharacterFeatApiResponse(
    FeatApiResponse Feat,
    List<SpellApiResponse> Spells);
