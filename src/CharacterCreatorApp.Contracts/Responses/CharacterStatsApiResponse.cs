namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// The parts of the sheet derived from the character rather than stored on it.
/// </summary>
public record CharacterStatsApiResponse(
    int ProficiencyBonus,
    int MaxHitPoints,
    int ArmorClass,
    int Initiative,
    int Speed,
    string HitDice,
    int PassivePerception,
    List<AbilityStatApiResponse> Abilities,
    List<SavingThrowStatApiResponse> SavingThrows,
    List<SkillStatApiResponse> Skills);

public record AbilityStatApiResponse(string AbilityId, int Score, int Modifier);

public record SavingThrowStatApiResponse(string AbilityId, int Modifier, bool IsProficient);

public record SkillStatApiResponse(string SkillId, string Name, string AbilityId, int Modifier, bool IsProficient);
