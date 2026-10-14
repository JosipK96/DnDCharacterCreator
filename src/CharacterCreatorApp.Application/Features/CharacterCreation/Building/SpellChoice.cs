using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// What a character may pick in the spells step, and whether a pick fits it. The spells step and
/// finalize both check through here, so going back and changing the level, class or subclass
/// cannot leave a pick finalize would accept but the step would not.
/// </summary>
public static class SpellChoice
{
    /// <summary>
    /// The class's own list, the whole list of another class a subclass opens up (the Wizard's
    /// for an Eldritch Knight, whose fighter has none), and the spells a subclass adds to its
    /// class's. Checking the class list alone refused every Eldritch Knight spell.
    /// </summary>
    public static async Task<List<SpellReadModel>> LoadChoosableAsync(
        ISpellRepository spellRepository, Class @class, Subclass? subclass, CancellationToken cancellationToken)
    {
        List<SpellReadModel> spells = [.. await spellRepository.GetByClassIdAsync(@class.Id, cancellationToken)];

        if (subclass?.SpellListClassId is { } listClassId)
        {
            spells.AddRange(await spellRepository.GetByClassIdAsync(listClassId, cancellationToken));
        }

        var addedIds = subclass?.AddedSpellIds.ToList() ?? [];

        if (addedIds.Count != 0)
        {
            spells.AddRange(await spellRepository.GetByIdsAsync(addedIds, cancellationToken));
        }

        return [.. spells.DistinctBy(s => s.Id)];
    }

    /// <summary>
    /// Returns null when the picks are exactly what the spell table gives this level: every one
    /// on the lists, none twice, the right number of cantrips and of spells, none above the
    /// highest spell level, and no more from outside a subclass's schools than it may take (an
    /// Eldritch Knight's abjuration and evocation). A character who casts nothing yet may pick
    /// nothing.
    /// </summary>
    public static Error? FindInvalidPick(
        IReadOnlyList<string> spellIds,
        IReadOnlyCollection<SpellReadModel> choosable,
        SpellAllowance? allowance,
        string className,
        int level)
    {
        if (allowance == null)
        {
            return spellIds.Count == 0 ? null : SpellErrors.NoSpellcasting(className, level);
        }

        var choosableById = choosable.ToDictionary(s => s.Id);

        if (!spellIds.All(choosableById.ContainsKey))
        {
            return SpellErrors.Invalid();
        }

        var picked = spellIds.Select(id => choosableById[id]).ToList();

        if (picked.GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
        {
            return SpellErrors.Duplicate(duplicate.First().Name);
        }

        var cantrips = picked.Count(s => s.Level == 0);

        if (cantrips != allowance.Cantrips)
        {
            return SpellErrors.CantripCountMismatch(className, level, allowance.Cantrips, cantrips);
        }

        if (picked.Find(s => s.Level > allowance.MaxSpellLevel) is { } tooHigh)
        {
            return SpellErrors.LevelTooHigh(tooHigh.Name, tooHigh.Level, className, level, allowance.MaxSpellLevel);
        }

        var leveled = picked.Count - cantrips;

        if (leveled != allowance.Spells)
        {
            return SpellErrors.SpellCountMismatch(className, level, allowance.Spells, leveled);
        }

        return allowance.SchoolLimit is { } limit ? FindOutsideSchools(picked, limit, className, level) : null;
    }

    // Cantrips come from any school
    private static Error? FindOutsideSchools(List<SpellReadModel> picked, SpellSchoolLimit limit, string className, int level)
    {
        var outside = picked.Where(s => s.Level > 0 && !limit.Allows(s.School)).Select(s => s.Name).ToList();

        return outside.Count <= limit.AnySchoolSpells
            ? null
            : SpellErrors.SchoolNotAllowed(className, level, limit.Schools, limit.AnySchoolSpells, outside);
    }
}
