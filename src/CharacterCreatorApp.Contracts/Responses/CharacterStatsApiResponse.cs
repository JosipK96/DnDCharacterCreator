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
    List<SkillStatApiResponse> Skills,
    List<SpellSlotStatApiResponse> SpellSlots,
    PactMagicStatApiResponse? PactMagic);

public record AbilityStatApiResponse(string AbilityId, int Score, int Modifier);

public record SavingThrowStatApiResponse(string AbilityId, int Modifier, bool IsProficient);

public record SkillStatApiResponse(string SkillId, string Name, string AbilityId, int Modifier, bool IsProficient);

/// <summary>The slots of one spell level, back on a long rest.</summary>
public record SpellSlotStatApiResponse(int SpellLevel, int Slots);

/// <summary>A warlock's Pact Magic slots, all of one spell level and back on a short rest.</summary>
public record PactMagicStatApiResponse(int SlotLevel, int Slots);
