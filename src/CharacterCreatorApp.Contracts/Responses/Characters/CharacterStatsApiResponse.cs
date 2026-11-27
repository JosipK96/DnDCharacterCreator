namespace CharacterCreatorApp.Contracts.Responses.Characters;

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
    PactMagicStatApiResponse? PactMagic,
    List<SpellcastingStatApiResponse> Spellcasting,
    List<SpellAttackStatApiResponse> Attacks);

public record AbilityStatApiResponse(string AbilityId, int Score, int Modifier);

public record SavingThrowStatApiResponse(string AbilityId, int Modifier, bool IsProficient);

public record SkillStatApiResponse(string SkillId, string Name, string AbilityId, int Modifier, bool IsProficient);

/// <summary>The slots of one spell level, back on a long rest.</summary>
public record SpellSlotStatApiResponse(int SpellLevel, int Slots);

/// <summary>A warlock's Pact Magic slots, all of one spell level and back on a short rest.</summary>
public record PactMagicStatApiResponse(int SlotLevel, int Slots);

/// <summary>The spell save DC and attack bonus of one spellcasting ability, labelled with what casts with it ("Wizard").</summary>
public record SpellcastingStatApiResponse(string Label, string AbilityId, int SaveDc, int AttackBonus);

/// <summary>
/// A damaging cantrip in the attacks table: its attack bonus, or its save DC and the saving
/// throw's ability ("DEX"), and its damage dice at the character's level ("2d10") and type ("fire").
/// </summary>
public record SpellAttackStatApiResponse(string Name, int? AttackBonus, int? SaveDc, string? SaveAbilityId, string Damage, string DamageType);
