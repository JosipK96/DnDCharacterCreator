namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// The numbers a character sheet shows that are derived from the character rather than stored:
/// modifiers, hit points, armour class, saves, skills and spell slots.
/// </summary>
public sealed record CharacterStats(
    int ProficiencyBonus,
    int MaxHitPoints,
    int ArmorClass,
    int Initiative,
    int Speed,
    string HitDice,
    int PassivePerception,
    IReadOnlyList<AbilityStat> Abilities,
    IReadOnlyList<SavingThrowStat> SavingThrows,
    IReadOnlyList<SkillStat> Skills,
    IReadOnlyList<SpellSlotStat> SpellSlots,
    PactMagicStat? PactMagic);

/// <summary>An ability score and the modifier derived from it.</summary>
public sealed record AbilityStat(string AbilityId, int Score, int Modifier);

/// <summary>A saving throw, including the proficiency bonus when the class is proficient.</summary>
public sealed record SavingThrowStat(string AbilityId, int Modifier, bool IsProficient);

/// <summary>A skill check, including the proficiency bonus when the character is proficient.</summary>
public sealed record SkillStat(string SkillId, string Name, string AbilityId, int Modifier, bool IsProficient);

/// <summary>The slots of one spell level the character has, back on a long rest.</summary>
public sealed record SpellSlotStat(int SpellLevel, int Slots);

/// <summary>A warlock's Pact Magic slots, all of one spell level and back on a short rest.</summary>
public sealed record PactMagicStat(int SlotLevel, int Slots);
