using CharacterCreatorApp.Domain.DomainModels.Classes;

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
    SpellSlots SpellSlots);
