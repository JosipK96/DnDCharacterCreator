namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// Spells the draft's race (FeatId null) or one of its feats lets the player pick: Cantrips and
/// FirstLevelSpells from one of Lists, each with the spells it offers from the allowed books.
/// </summary>
public record BonusSpellChoiceApiResponse(string? FeatId, string Name, int Cantrips, int FirstLevelSpells, List<BonusSpellListApiResponse> Lists);

public record BonusSpellListApiResponse(string ClassId, string ClassName, List<SpellApiResponse> Spells);
