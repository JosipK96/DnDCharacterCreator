using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Finalizing;

/// <summary>
/// What a character casts its race's and feats' picked spells with, for the sheet's save DC and
/// attack bonus beside the class's, and what each damaging cantrip is cast with, for its attacks
/// table. The review step and the sheet both come here.
/// </summary>
public static class BonusSpellcasting
{
    /// <summary>
    /// One source for the race's spells, labelled with the subrace or race, then one per feat
    /// spell pick, labelled with the feat. Where the race or feat lets the player pick the ability
    /// (a 2024 high elf, the 2024 Magic Initiate) the pick is used. Otherwise a race's spells are
    /// cast with its own ability (a tiefling's Charisma), and spells picked from a class's list
    /// with that class's (the 2014 Magic Initiate, the 2014 high elf's wizard cantrip).
    /// </summary>
    public static async Task<List<SpellcastingSource>> LoadAsync(
        Race race,
        Subrace? subrace,
        string? raceSpellAbilityId,
        IEnumerable<FeatSpellPick> featSpells,
        IEnumerable<CharacterFeatReadModel> feats,
        IClassRepository classRepository,
        CancellationToken cancellationToken)
    {
        var sources = new List<SpellcastingSource>();
        var abilityByClassId = new Dictionary<string, string?>(StringComparer.Ordinal);

        var raceAbility = await RaceAbilityAsync(race, subrace, raceSpellAbilityId, abilityByClassId, classRepository, cancellationToken);

        if (raceAbility != null)
        {
            sources.Add(new SpellcastingSource(subrace?.Name ?? race.Name, raceAbility));
        }

        var featNames = feats.Select(f => f.Feat).DistinctBy(f => f.Id).ToDictionary(f => f.Id, f => f.Name);

        foreach (var pick in featSpells)
        {
            var ability = pick.AbilityId ?? await ClassAbilityAsync(pick.ClassId, abilityByClassId, classRepository, cancellationToken);

            if (ability != null)
            {
                sources.Add(new SpellcastingSource(featNames.GetValueOrDefault(pick.FeatId, pick.FeatId), ability));
            }
        }

        return sources;
    }

    /// <summary>
    /// The character's damaging cantrips (<see cref="CantripDamage.IsAttackCantrip"/>), each with
    /// the ability it is cast with: a feat's pick with the feat's, the race's own with the race's,
    /// one on the class's list or given by the subclass with the class's, and any other (a race's
    /// pick from another class's list, a high elf fighter's Fire Bolt) with the race's. A cantrip
    /// cast with no ability the character has is left out.
    /// </summary>
    public static async Task<List<AttackCantrip>> AttackCantripsAsync(
        IEnumerable<SpellReadModel> spells,
        int level,
        Class @class,
        Subclass? subclass,
        Race race,
        Subrace? subrace,
        string? raceSpellAbilityId,
        IEnumerable<FeatSpellPick> featSpells,
        IClassRepository classRepository,
        CancellationToken cancellationToken)
    {
        var cantrips = spells
            .Where(s => CantripDamage.IsAttackCantrip(s.Level, s.AttackType, s.SaveAbilityId, s.DamageType, s.CantripDamageDice))
            .ToList();

        if (cantrips.Count == 0)
        {
            return [];
        }

        var abilityByClassId = new Dictionary<string, string?>(StringComparer.Ordinal);
        var classAbility = @class.SpellcastingAt(level, subclass)?.AbilityId;
        var raceAbility = await RaceAbilityAsync(race, subrace, raceSpellAbilityId, abilityByClassId, classRepository, cancellationToken);
        var raceSpellIds = race.SpellIdsAt(level, subrace).ToHashSet(StringComparer.Ordinal);
        var subclassSpellIds = subclass?.Spells.Select(s => s.SpellId).ToHashSet(StringComparer.Ordinal) ?? [];
        var picks = featSpells.ToList();
        var attackCantrips = new List<AttackCantrip>();

        foreach (var spell in cantrips)
        {
            var featPick = picks.Find(pick => pick.SpellIds.Contains(spell.Id));
            var featAbility = featPick == null
                ? null
                : featPick.AbilityId ?? await ClassAbilityAsync(featPick.ClassId, abilityByClassId, classRepository, cancellationToken);
            var isClassSpell = spell.ClassIds.Contains(@class.Id) || subclassSpellIds.Contains(spell.Id);

            // shortcut: a race's pick that is also on the class's list is taken for the class's, as
            // the character does not keep which pick gave it; keep the race's picks on the
            // character if a race ever casts one with another ability than the class
            var ability = featAbility
                ?? (raceSpellIds.Contains(spell.Id) ? raceAbility : null)
                ?? (isClassSpell ? classAbility : null)
                ?? raceAbility
                ?? classAbility;

            if (ability != null)
            {
                attackCantrips.Add(new AttackCantrip(
                    spell.Name, ability, spell.AttackType, spell.SaveAbilityId, spell.DamageType!, spell.CantripDamageDice));
            }
        }

        return attackCantrips;
    }

    // The race's own ability, or the one the player picked for its spells, or, for a race whose
    // spells are picked from one class's list with no ability to pick, that class's
    private static async Task<string?> RaceAbilityAsync(
        Race race,
        Subrace? subrace,
        string? raceSpellAbilityId,
        Dictionary<string, string?> abilityByClassId,
        IClassRepository classRepository,
        CancellationToken cancellationToken)
    {
        var raceSlot = BonusSpells.RaceSlot(race, subrace);

        return raceSpellAbilityId
            ?? race.SpellcastingAbilityWith(subrace)
            ?? (raceSlot is { Choice: { ClassIds: [var raceListClassId], AbilityOptions.Count: 0 } }
                ? await ClassAbilityAsync(raceListClassId, abilityByClassId, classRepository, cancellationToken)
                : null);
    }

    private static async Task<string?> ClassAbilityAsync(
        string classId, Dictionary<string, string?> abilityByClassId, IClassRepository classRepository, CancellationToken cancellationToken)
    {
        if (!abilityByClassId.TryGetValue(classId, out var ability))
        {
            ability = (await classRepository.GetByIdAsync(classId, cancellationToken))?.Spellcasting?.SpellcastingAbility;
            abilityByClassId[classId] = ability;
        }

        return ability;
    }
}
