using CharacterCreatorApp.Contracts.Responses;

namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// Each ability score improvement is spent either on ability points (an entry in
/// <paramref name="Improvements"/>) or on a feat (an entry in <paramref name="FeatIds"/>). The
/// feats a race gives besides them (a variant human's) come first in FeatIds.
/// <paramref name="FeatAbilityIncreases"/> says what each feat raises, in the same order, where
/// the player picks it: {"DEX": 1} for an Athlete who chose Dexterity, {} for any other feat.
/// <paramref name="FeatProficiencyPicks"/> are the skills or tools picked for each feat that asks
/// for them (Keen Mind, Skilled): the background's feat first, then FeatIds' in order.
/// </summary>
public record SetAbilityScoreImprovementsApiRequest(
    List<Dictionary<string, int>> Improvements,
    List<string> FeatIds,
    List<Dictionary<string, int>>? FeatAbilityIncreases = null,
    List<FeatProficiencyPickApiModel>? FeatProficiencyPicks = null);
