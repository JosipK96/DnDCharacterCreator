using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Finalizing;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;

/// <summary>
/// What a character may pick in the spells step, and whether a pick fits it. The spells step and
/// finalize both check through here, so going back and changing the level, class or subclass
/// cannot leave a pick finalize would accept but the step would not.
/// </summary>
public static class SpellChoice
{
    /// <summary>
    /// The spells step's list for a draft, by level then name, and the spells it would offer to
    /// pick once its unticked books were ticked. Listed is what the lists offer
    /// (<see cref="LoadChoosableAsync"/>) from the draft's books up to maxSpellLevel, and the
    /// subclass's always-prepared spells, which the step names but are not picked and so show
    /// whatever the books (a Vengeance paladin's Hunter's Mark, off the class's list).
    /// InUntickedBooks is what the lists offer up to that level from the other books, less the
    /// always-prepared ones. GET spell-allowance builds both here, so the list and the count of
    /// what the unticked books hold cannot disagree.
    /// </summary>
    public static async Task<StepSpellList> LoadStepListAsync(
        ISpellRepository spellRepository,
        CharacterDraft draft,
        Class @class,
        Subclass? subclass,
        int level,
        int maxSpellLevel,
        CancellationToken cancellationToken)
    {
        var alwaysPrepared = subclass?.AlwaysPreparedSpellIdsAt(level).ToHashSet() ?? [];
        var choosable = await LoadChoosableAsync(spellRepository, @class, subclass, cancellationToken);

        var pickable = choosable.Where(s => s.Level <= maxSpellLevel && !alwaysPrepared.Contains(s.Id)).ToList();
        var listed = pickable.Where(s => draft.AllowsSource(s.Source)).ToList();
        var inUntickedBooks = pickable.Where(s => !draft.AllowsSource(s.Source)).ToList();

        var prepared = choosable.Where(s => alwaysPrepared.Contains(s.Id)).ToList();
        var unlisted = alwaysPrepared.Except(prepared.Select(s => s.Id)).ToList();

        if (unlisted.Count != 0)
        {
            prepared.AddRange(await spellRepository.GetByIdsAsync(unlisted, cancellationToken));
        }

        listed.AddRange(prepared);

        return new StepSpellList(
            [.. listed.OrderBy(s => s.Level).ThenBy(s => s.Name, StringComparer.Ordinal)],
            inUntickedBooks);
    }

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
    /// What a draft of this class, subclass and level picks in the spells step, or null when it
    /// casts nothing yet. A 2014 cleric's, druid's, paladin's or artificer's count follows its
    /// final scores, so for one of those the draft's sources are loaded and the character built,
    /// as finalize builds it; a draft without its race, background and scores cannot know it yet.
    /// </summary>
    public static async Task<Response<SpellAllowance?>> AllowanceForAsync(
        CharacterDraft draft,
        Class @class,
        Subclass? subclass,
        int level,
        DraftSourceLoader sourceLoader,
        Guid userId,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // Only a prepared caster's count reads the scores; the base ones do for everyone else
        if (!@class.PreparesSpellsFromScoresAt(level))
        {
            return @class.SpellAllowanceAt(level, subclass, draft.BaseAbilityScores);
        }

        if (!draft.IsReadyToFinalize())
        {
            return CharacterDraftErrors.ScoresRequired("selecting spells");
        }

        var sources = await sourceLoader.LoadAsync(draft, cancellationToken);

        if (!sources.IsSuccess)
        {
            return sources.Errors;
        }

        var character = CharacterBuilder.Build(userId, draft, sources.Value, timeProvider);

        return @class.SpellAllowanceAt(level, subclass, character.AbilityScores);
    }

    /// <summary>
    /// Returns null when the picks are exactly what the spell table gives this level: every one
    /// on the lists, none twice, none the subclass always has prepared (a Life cleric's Bless),
    /// the right number of cantrips and of spells, none above the highest spell level, and no
    /// more from outside a subclass's schools than it may take (an Eldritch Knight's abjuration
    /// and evocation). A character who casts nothing yet may pick nothing.
    /// </summary>
    public static Error? FindInvalidPick(
        IReadOnlyList<string> spellIds,
        IReadOnlyCollection<SpellReadModel> choosable,
        SpellAllowance? allowance,
        Subclass? subclass,
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

        // A domain or oath spell is prepared without counting against the picks: picking it again
        // would spend a pick on nothing
        var alwaysPrepared = subclass?.AlwaysPreparedSpellIdsAt(level).ToHashSet() ?? [];

        if (picked.Find(s => alwaysPrepared.Contains(s.Id)) is { } prepared)
        {
            return SpellErrors.AlwaysPrepared(prepared.Name, subclass!.Name);
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
