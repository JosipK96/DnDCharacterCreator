namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// Spells the draft's race (FeatId null) or one of its feats lets the player pick: Cantrips and
/// FirstLevelSpells from one of Lists, each with the spells it offers from the allowed books.
/// AbilityOptions are the spellcasting abilities to pick one of with them ("INT", "WIS", "CHA"
/// for the 2024 Magic Initiate), empty when there is no pick.
/// </summary>
public record BonusSpellChoiceApiResponse(
    string? FeatId, string Name, int Cantrips, int FirstLevelSpells, List<BonusSpellListApiResponse> Lists, List<string> AbilityOptions);

public record BonusSpellListApiResponse(string ClassId, string ClassName, List<SpellApiResponse> Spells);
