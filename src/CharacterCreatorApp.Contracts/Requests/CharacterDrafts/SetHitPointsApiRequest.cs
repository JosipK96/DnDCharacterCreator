namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

/// <summary>
/// The hit die's results by level, rolled or the die's fixed value, for the levels the draft's
/// HitDieLevels names: a level-up's new level, or the levels from 2 a new character or an edit
/// picked a result for. A level left out of those takes the fixed value.
/// </summary>
public record SetHitPointsApiRequest(Dictionary<int, int> Results);
