using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// What a character needs to supply for its derived numbers to be worked out. Hit die, saving
/// throws and speed come from the chosen class and race, which the character stores only by id.
/// Race, subrace, subclass and feats are here for the hit points they add (see HitPointBonuses),
/// and speed is the race's with its subrace's change (Race.SpeedWith). The unarmoured armour
/// classes are the class's and subclass's (Class.UnarmoredArmorClassesWith), and the spell slots
/// the class's or subclass's table row for the level (Class.SpellSlotsAt). The hit die results
/// are the levels a level-up rolled for, or took the fixed value of (Character.HitDieResults).
/// Nothing has a default, so a caller cannot leave a part out without the compiler saying so.
/// </summary>
public sealed record CharacterStatsInput(
    int Level,
    IReadOnlyDictionary<string, int> AbilityScores,
    IReadOnlyCollection<string> ProficiencyIds,
    string HitDie,
    IReadOnlyDictionary<int, int> HitDieResults,
    IReadOnlyCollection<string> SavingThrowAbilityIds,
    int Speed,
    string RaceId,
    string? SubraceId,
    string? SubclassId,
    IReadOnlyCollection<string> FeatIds,
    IReadOnlyCollection<UnarmoredArmorClass> UnarmoredArmorClasses,
    SpellSlots SpellSlots)
{
    /// <summary>
    /// The input for a character at its level: the class's saving throws joined with the ones
    /// feats add, the race's speed with its subrace's change, and the class's and subclass's
    /// armour classes and spell slots. The review step and the saved sheet both come here, so
    /// they cannot work the same character out differently.
    /// </summary>
    public static CharacterStatsInput For(
        int level,
        IReadOnlyDictionary<string, int> abilityScores,
        IReadOnlyCollection<string> proficiencyIds,
        IReadOnlyDictionary<int, int> hitDieResults,
        IReadOnlyCollection<string> featIds,
        IEnumerable<string> featSavingThrowAbilityIds,
        Race race,
        Subrace? subrace,
        Class @class,
        Subclass? subclass) =>
        new(
            Level: level,
            AbilityScores: abilityScores,
            ProficiencyIds: proficiencyIds,
            HitDie: @class.HitDie,
            HitDieResults: hitDieResults,
            SavingThrowAbilityIds: @class.SavingThrowAbilityIds.Concat(featSavingThrowAbilityIds).Distinct().ToList(),
            Speed: race.SpeedWith(subrace),
            RaceId: race.Id,
            SubraceId: subrace?.Id,
            SubclassId: subclass?.Id,
            FeatIds: featIds,
            UnarmoredArmorClasses: @class.UnarmoredArmorClassesWith(subclass),
            SpellSlots: @class.SpellSlotsAt(level, subclass));
}
