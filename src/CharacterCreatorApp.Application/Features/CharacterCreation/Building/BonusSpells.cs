using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// The spells a race or feat lets the player pick from a class list (a 2014 high elf's wizard
/// cantrip, Magic Initiate's two cantrips and 1st-level spell), and whether the picks fit. The
/// bonus spells step and finalize both check through here.
/// </summary>
public static class BonusSpells
{
    /// <summary>The race's or subrace's pick, or null when neither has one.</summary>
    public static BonusSpellSlot? RaceSlot(Race race, Subrace? subrace) =>
        race.SpellChoiceWith(subrace) is { } choice ? new BonusSpellSlot(null, subrace?.Name ?? race.Name, choice) : null;

    /// <summary>
    /// One pick for each feat with a spell choice, in the character's order: the background's
    /// feat, the Fighting Styles, then the race's and the improvements' feats. A background that
    /// fixes its feat's list (an Acolyte's Magic Initiate, the cleric's) leaves only that list.
    /// </summary>
    public static List<BonusSpellSlot> FeatSlots(CharacterDraft draft, Background? background, IReadOnlyDictionary<string, FeatReadModel> featsById)
    {
        var slots = new List<BonusSpellSlot>();

        foreach (var featId in background == null ? [] : CharacterBuilder.BackgroundFeatIds(background, draft))
        {
            AddSlot(slots, featId, featsById, background!.FeatSpellListClassId);
        }

        foreach (var featId in draft.FightingStyleIds.Concat(draft.SelectedFeatIds))
        {
            AddSlot(slots, featId, featsById, fixedClassId: null);
        }

        return slots;
    }

    /// <summary>
    /// The race's slot and the feats' slots of a draft that may be part way through: no race or
    /// background yet means nothing from it.
    /// </summary>
    public static async Task<(BonusSpellSlot? Race, List<BonusSpellSlot> Feats)> LoadSlotsAsync(
        CharacterDraft draft,
        IRaceRepository raceRepository,
        IBackgroundRepository backgroundRepository,
        IFeatRepository featRepository,
        CancellationToken cancellationToken)
    {
        var race = draft.RaceId is { } raceId ? await raceRepository.GetByIdWithSubracesAsync(raceId, cancellationToken) : null;
        var background = draft.BackgroundId is { } backgroundId ? await backgroundRepository.GetByIdWithDetailsAsync(backgroundId, cancellationToken) : null;
        var featIds = (background == null ? [] : CharacterBuilder.BackgroundFeatIds(background, draft))
            .Concat(draft.FightingStyleIds)
            .Concat(draft.SelectedFeatIds)
            .Distinct();
        var feats = await featRepository.GetByIdsAsync(featIds, cancellationToken);

        var raceSlot = race == null ? null : RaceSlot(race, race.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId));

        return (raceSlot, FeatSlots(draft, background, feats.ToDictionary(f => f.Id)));
    }

    /// <summary>Every class list the slots offer, by class id, for FindInvalidPick to check against.</summary>
    public static async Task<Dictionary<string, List<SpellReadModel>>> LoadListsAsync(
        ISpellRepository spellRepository, IEnumerable<BonusSpellSlot> slots, CancellationToken cancellationToken)
    {
        var lists = new Dictionary<string, List<SpellReadModel>>();

        foreach (var classId in slots.SelectMany(slot => slot.Choice.ClassIds).Distinct())
        {
            lists[classId] = await spellRepository.GetByClassIdAsync(classId, cancellationToken);
        }

        return lists;
    }

    /// <summary>
    /// Returns null when the race's spells and one pick per feat slot, in order, are exactly what
    /// each gives: a list the feat offers (another each time a repeatable one is taken again),
    /// the cantrips and 1st-level spells it counts, every one on that list, from an allowed book,
    /// none twice.
    /// </summary>
    public static Error? FindInvalidPick(
        CharacterDraft draft,
        BonusSpellSlot? raceSlot,
        IReadOnlyList<BonusSpellSlot> featSlots,
        IReadOnlyList<string> raceSpellIds,
        IReadOnlyList<FeatSpellPick> featSpells,
        IReadOnlyDictionary<string, List<SpellReadModel>> lists)
    {
        if (raceSlot == null)
        {
            if (raceSpellIds.Count != 0)
            {
                return CharacterDraftErrors.BonusSpellCount("The race", 0, 0, raceSpellIds.Count, 0);
            }
        }
        else if (FindInvalidSpells(draft, raceSlot, raceSlot.Choice.ClassIds, raceSpellIds, lists) is { } raceError)
        {
            return raceError;
        }

        if (featSpells.Count != featSlots.Count || featSpells.Where((pick, i) => pick.FeatId != featSlots[i].FeatId).Any())
        {
            return CharacterDraftErrors.BonusSpellPicksMismatch(featSlots.Select(s => s.Name).ToList(), featSpells.Count);
        }

        for (var i = 0; i < featSlots.Count; i++)
        {
            var (slot, pick) = (featSlots[i], featSpells[i]);

            if (!slot.Choice.ClassIds.Contains(pick.ClassId))
            {
                return CharacterDraftErrors.BonusSpellListNotOffered(slot.Name, pick.ClassId, slot.Choice.ClassIds);
            }

            if (featSpells.Take(i).Any(earlier => earlier.FeatId == pick.FeatId && earlier.ClassId == pick.ClassId))
            {
                return CharacterDraftErrors.BonusSpellListRepeated(slot.Name, pick.ClassId);
            }

            if (FindInvalidSpells(draft, slot, [pick.ClassId], pick.SpellIds, lists) is { } featError)
            {
                return featError;
            }

            if (!AllowsAbility(slot.Choice, pick.AbilityId))
            {
                return CharacterDraftErrors.InvalidSpellcastingAbility(
                    slot.Name, slot.Choice.AbilityOptions.ConvertAll(id => AbilityScore.FromId(id).Name));
            }
        }

        return null;
    }

    private static void AddSlot(List<BonusSpellSlot> slots, string featId, IReadOnlyDictionary<string, FeatReadModel> featsById, string? fixedClassId)
    {
        if (featsById.GetValueOrDefault(featId) is not { SpellChoice: { } choice } feat)
        {
            return;
        }

        if (fixedClassId != null)
        {
            choice = new BonusSpellChoice
            {
                ClassIds = [fixedClassId],
                Cantrips = choice.Cantrips,
                FirstLevelSpells = choice.FirstLevelSpells,
                AbilityOptions = choice.AbilityOptions,
            };
        }

        slots.Add(new BonusSpellSlot(featId, feat.Name, choice));
    }

    // The 2024 Magic Initiate's spellcasting ability, in any spelling; none where the feat offers none
    private static bool AllowsAbility(BonusSpellChoice choice, string? abilityId) =>
        abilityId is null
            ? choice.AbilityOptions.Count == 0
            : AbilityScore.TryNormalize(abilityId, out var id) && choice.AbilityOptions.Contains(id);

    private static Error? FindInvalidSpells(
        CharacterDraft draft,
        BonusSpellSlot slot,
        IReadOnlyList<string> classIds,
        IReadOnlyList<string> spellIds,
        IReadOnlyDictionary<string, List<SpellReadModel>> lists)
    {
        var onList = classIds.SelectMany(id => lists.GetValueOrDefault(id) ?? []).DistinctBy(s => s.Id).ToDictionary(s => s.Id);

        if (spellIds.FirstOrDefault(id => !onList.ContainsKey(id)) is { } notOnList)
        {
            return CharacterDraftErrors.BonusSpellNotOnList(slot.Name, notOnList, string.Join(" or ", classIds));
        }

        var picked = spellIds.Select(id => onList[id]).ToList();

        if (picked.GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
        {
            return SpellErrors.Duplicate(duplicate.First().Name);
        }

        if (picked.Find(s => !draft.AllowsSource(s.Source)) is { } notAllowed)
        {
            return CharacterDraftErrors.SourceNotAllowed("spell", notAllowed.Name, notAllowed.Source);
        }

        var cantrips = picked.Count(s => s.Level == 0);
        var firstLevel = picked.Count(s => s.Level == 1);

        return cantrips == slot.Choice.Cantrips && firstLevel == slot.Choice.FirstLevelSpells && cantrips + firstLevel == picked.Count
            ? null
            : CharacterDraftErrors.BonusSpellCount(slot.Name, slot.Choice.Cantrips, slot.Choice.FirstLevelSpells, cantrips, picked.Count - cantrips);
    }
}
