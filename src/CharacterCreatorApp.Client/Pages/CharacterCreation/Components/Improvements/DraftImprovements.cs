using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Contracts.Responses.Feats;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components.Improvements;

/// <summary>
/// Translates between the draft as the API keeps it and the slots the improvements step shows:
/// rebuilding the slots when the step opens, and turning them back into what the step saves.
/// </summary>
public static class DraftImprovements
{
    /// <summary>
    /// Rebuilds the slots from what was saved. The draft stores the race's feats first among the
    /// feats, then improvements and feats as two lists without recording which slot each came
    /// from, so improvements are restored first and feats fill the slots after them. When what was
    /// saved no longer fits what the character has earned, the slots start empty.
    /// </summary>
    /// <param name="draft">The draft as the API sent it.</param>
    /// <param name="bonusFeatCount">The feats the race gives (a variant human's), which come first.</param>
    /// <param name="asiCount">The improvements the class has given by the character's level.</param>
    public static List<AsiSlot> RestoreSlots(CharacterDraftDetailsApiResponse draft, int bonusFeatCount, int asiCount)
    {
        var featIds = draft.SelectedFeatIds ?? [];
        var increases = draft.FeatAbilityIncreases ?? [];
        var saved = new List<AsiSlot>();

        for (var i = 0; i < Math.Min(bonusFeatCount, featIds.Count); i++)
        {
            saved.Add(FeatSlot(featIds[i], increases.ElementAtOrDefault(i), isRaceFeat: true));
        }

        foreach (var improvement in draft.AbilityScoreImprovements ?? [])
        {
            saved.Add(PointsSlot(improvement));
        }

        for (var i = bonusFeatCount; i < featIds.Count; i++)
        {
            saved.Add(FeatSlot(featIds[i], increases.ElementAtOrDefault(i), isRaceFeat: false));
        }

        // Only trust the restored slots if they still fit what this character has earned. A
        // level-up's new improvement comes after the ones already taken, which stay as they were.
        var taken = saved.Count(s => !s.IsRaceFeat);

        if (saved.Count(s => s.IsRaceFeat) == bonusFeatCount && taken <= asiCount)
        {
            return [.. saved, .. Enumerable.Range(0, asiCount - taken).Select(_ => new AsiSlot())];
        }

        return
        [
            .. Enumerable.Range(0, bonusFeatCount).Select(_ => new AsiSlot { IsRaceFeat = true, TakesFeat = true }),
            .. Enumerable.Range(0, asiCount).Select(_ => new AsiSlot()),
        ];
    }

    /// <summary>
    /// Puts the saved skill and tool picks back, one per feat that asks, in the order the API keeps
    /// them: the background's feat, then the feats in slot order. A pick for another feat is dropped.
    /// Each slot's picks are set on it; the background feat's are returned, as the step keeps them.
    /// </summary>
    /// <param name="draft">The draft as the API sent it.</param>
    /// <param name="backgroundFeat">The background's feat when it asks for skills or tools (a Noble's Skilled).</param>
    /// <param name="slots">The slots <see cref="RestoreSlots"/> rebuilt.</param>
    /// <param name="feats">The feats the slots can take, to find what each slot's feat asks for.</param>
    public static List<string?> RestoreProficiencyPicks(
        CharacterDraftDetailsApiResponse draft,
        FeatApiResponse? backgroundFeat,
        IEnumerable<AsiSlot> slots,
        IReadOnlyCollection<FeatApiResponse> feats)
    {
        var saved = new Queue<FeatProficiencyPickApiModel>(draft.FeatProficiencyPicks ?? []);
        List<string?> backgroundFeatPicks = [];

        if (backgroundFeat is { Choice: { } backgroundChoice })
        {
            backgroundFeatPicks = RestoredPicks(backgroundFeat.Id, backgroundChoice, saved);
        }

        foreach (var slot in slots.Where(s => s.TakesFeat))
        {
            if (feats.FirstOrDefault(f => f.Id == slot.FeatId) is { Choice: { } choice } feat && choice.AsksForProficiencies())
            {
                slot.ProficiencyPicks = RestoredPicks(feat.Id, choice, saved);
            }
        }

        return backgroundFeatPicks;
    }

    /// <summary>
    /// The ability points every improvement spent on points has so far, a half-spent one included:
    /// what the API needs to say which feats the character can take.
    /// </summary>
    public static Dictionary<string, int> SpentPoints(IEnumerable<AsiSlot> slots) =>
        slots
            .Where(s => !s.TakesFeat)
            .SelectMany(s => s.Improvement)
            .GroupBy(pair => pair.Key)
            .ToDictionary(points => points.Key, points => points.Sum(pair => pair.Value));

    /// <summary>
    /// What the step saves, in the order the API expects: the race's feats first (they are the
    /// first slots), then the improvements' in order; the background feat's picks before the
    /// picks of each feat that asks.
    /// </summary>
    public static ImprovementsToSave ToSave(
        IReadOnlyList<AsiSlot> slots,
        IReadOnlyCollection<FeatApiResponse> feats,
        FeatApiResponse? backgroundFeat,
        IEnumerable<string?> backgroundFeatPicks)
    {
        var improvements = slots
            .Where(s => !s.TakesFeat)
            .Select(s => new Dictionary<string, int>(s.Improvement))
            .ToList();

        var featSlots = slots
            .Where(s => s.TakesFeat && !string.IsNullOrEmpty(s.FeatId))
            .ToList();

        var featIds = featSlots.Select(s => s.FeatId!).ToList();
        var featAbilityIncreases = featSlots.Select(s => s.FeatIncrease(feats.FirstOrDefault(f => f.Id == s.FeatId))).ToList();

        var proficiencyPicks = new List<FeatProficiencyPickApiModel>();

        if (backgroundFeat is not null)
        {
            proficiencyPicks.Add(new FeatProficiencyPickApiModel { FeatId = backgroundFeat.Id, ProficiencyIds = [.. backgroundFeatPicks.OfType<string>()] });
        }

        proficiencyPicks.AddRange(featSlots
            .Where(s => s.ProficiencyPicks.Count > 0)
            .Select(s => new FeatProficiencyPickApiModel { FeatId = s.FeatId!, ProficiencyIds = [.. s.ProficiencyPicks.OfType<string>()] }));

        return new ImprovementsToSave(improvements, featIds, featAbilityIncreases, proficiencyPicks);
    }

    // A saved improvement's points, keyed by the canonical ids whatever spelling it was saved in
    private static AsiSlot PointsSlot(Dictionary<string, int> improvement)
    {
        var slot = new AsiSlot();

        foreach (var entry in improvement)
        {
            var abilityId = AbilityNames.IdOf(entry.Key);
            slot.Improvement[abilityId] = slot.PointsIn(abilityId) + entry.Value;
        }

        return slot;
    }

    // A saved feat and the ability it raises, the one key of what was saved ({"DEX": 1})
    private static AsiSlot FeatSlot(string featId, Dictionary<string, int>? increase, bool isRaceFeat) => new()
    {
        IsRaceFeat = isRaceFeat,
        TakesFeat = true,
        FeatId = featId,
        FeatAbility = increase?.FirstOrDefault(kvp => kvp.Value > 0).Key,
    };

    private static List<string?> RestoredPicks(string featId, FeatChoiceApiResponse choice, Queue<FeatProficiencyPickApiModel> saved)
    {
        var ids = saved.TryPeek(out var next) && next.FeatId == featId ? saved.Dequeue().ProficiencyIds : [];
        return Enumerable.Range(0, choice.Count).Select(i => ids.ElementAtOrDefault(i)).ToList();
    }
}

/// <summary>The improvements step's save, as <c>CharacterDraftService.SetAbilityScoreImprovementsAsync</c> takes it.</summary>
public sealed record ImprovementsToSave(
    List<Dictionary<string, int>> Improvements,
    List<string> FeatIds,
    List<Dictionary<string, int>> FeatAbilityIncreases,
    List<FeatProficiencyPickApiModel> ProficiencyPicks);
