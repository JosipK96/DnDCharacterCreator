namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// The numbers a character sheet shows that are derived from the character rather than stored:
/// modifiers, hit points, armour class, saves and skills.
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
    IReadOnlyList<SkillStat> Skills);

/// <summary>An ability score and the modifier derived from it.</summary>
public sealed record AbilityStat(string AbilityId, int Score, int Modifier);

/// <summary>A saving throw, including the proficiency bonus when the class is proficient.</summary>
public sealed record SavingThrowStat(string AbilityId, int Modifier, bool IsProficient);

/// <summary>A skill check, including the proficiency bonus when the character is proficient.</summary>
public sealed record SkillStat(string SkillId, string Name, string AbilityId, int Modifier, bool IsProficient);
