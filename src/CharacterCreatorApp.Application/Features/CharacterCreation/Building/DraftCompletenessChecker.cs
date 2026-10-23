using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Checks the choices whose required count only becomes known once the race, class and
/// background are loaded. A draft can reach finalize with a step skipped entirely, so each
/// count is compared against what the character is actually entitled to.
/// </summary>
public static class DraftCompletenessChecker
{
    /// <summary>
    /// Returns null when every choice the character is owed has been made. The scores are the
    /// built character's, which a 2014 cleric's number of prepared spells follows.
    /// </summary>
    public static Error? FindMissingChoice(CharacterDraft draft, CharacterSources sources, IReadOnlyDictionary<string, int> finalAbilityScores)
    {
        var (race, subrace, @class, background) = (sources.Race, sources.Subrace, sources.Class, sources.Background);
        var raceName = subrace?.Name ?? race.Name;
        var level = draft.Level!.Value;

        // The scores must be ones the method the player chose can produce; a draft saved before
        // the method was recorded has to choose them again
        if (draft.BaseAbilityScoreMethod is not { } method)
        {
            return CharacterErrors.AbilityScoreMethodRequired();
        }

        if (!AbilityScoreMethod.Allows(method, draft.BaseAbilityScores.Values))
        {
            return CharacterErrors.AbilityScoresDoNotFitMethod(method);
        }

        // Narrowing the books clears what came from them, but a step may have been saved under
        // books the draft allowed before
        var notAllowed = FindChoiceFromBookNotAllowed(draft, sources);

        if (notAllowed is not null)
        {
            return notAllowed;
        }

        // A dwarf is a hill or a mountain dwarf, a 2024 elf has a lineage; a 2014 human with
        // neither the standard nor the variant choice would have no increases at all
        if (subrace == null && race.Subraces.Count != 0)
        {
            return CharacterErrors.SubraceRequired(race.Name, race.Subraces.Select(s => s.Name));
        }

        // A 2024 human, aasimar or tiefling is Small or Medium, as the player picks
        var sizes = race.SizeOptions();

        if (draft.Size is null ? sizes.Count > 1 : !sizes.Contains(draft.Size))
        {
            return CharacterErrors.SizeRequired(race.Name, sizes);
        }

        var backgroundFeatError = FindInvalidBackgroundFeat(draft, sources);

        if (backgroundFeatError is not null)
        {
            return backgroundFeatError;
        }

        if (level >= @class.SubclassLevel && string.IsNullOrWhiteSpace(draft.SubclassId))
        {
            return CharacterErrors.SubclassRequired(level, @class.Name, @class.SubclassLevel);
        }

        var hitPointsError = FindInvalidHitDieResult(draft, @class.HitDie, level);

        if (hitPointsError is not null)
        {
            return hitPointsError;
        }

        var requiredProficiencies = @class.ProficiencyChoice?.Count ?? 0;

        if (draft.SelectedClassProficiencyIds.Count != requiredProficiencies)
        {
            return CharacterErrors.ProficiencyChoicesIncomplete(@class.Name, requiredProficiencies, draft.SelectedClassProficiencyIds.Count);
        }

        var requiredRaceSkills = race.SkillChoiceCountWith(subrace);

        if (draft.SelectedRaceSkillIds.Count != requiredRaceSkills)
        {
            return CharacterErrors.RaceSkillChoicesIncomplete(raceName, requiredRaceSkills, draft.SelectedRaceSkillIds.Count);
        }

        var raceAbilityChoice = race.AbilityChoiceWith(subrace);

        if (!raceAbilityChoice.Allows(draft.RaceAbilityIncreases))
        {
            return CharacterErrors.RaceAbilityIncreasesIncomplete(raceName, raceAbilityChoice.Points);
        }

        // A 2024 background's increases are the character's only ones: its species gives none
        if (!background.AllowsAbilityIncreases(draft.BackgroundAbilityIncreases))
        {
            return CharacterErrors.BackgroundAbilityIncreasesIncomplete(background.Name);
        }

        var backgroundLanguageChoices = background.Languages
            .Count(l => l.Equals("any", StringComparison.OrdinalIgnoreCase));
        var requiredLanguages = race.LanguageChoiceCountWith(subrace) + backgroundLanguageChoices;

        if (draft.SelectedLanguageIds.Count != requiredLanguages)
        {
            return CharacterErrors.LanguageChoicesIncomplete(race.Name, background.Name, requiredLanguages, draft.SelectedLanguageIds.Count);
        }

        // A dwarf's artisan's tools, a Criminal's gaming set, a bard's instruments
        var toolError = ToolPicks.FindInvalidPick(draft.ToolPicks, ToolPicks.SlotsFor(race, background, @class));

        if (toolError is not null)
        {
            return toolError;
        }

        // Keen Mind's skill, Skilled's three: another background or set of feats clears them
        var featProficiencyError = FeatProficiencyPicks.FindInvalidPick(
            draft.FeatProficiencyPicks,
            FeatProficiencyPicks.SlotsFor(CharacterBuilder.BackgroundFeatIds(background, draft), draft.SelectedFeatIds, sources.Feats.ToDictionary(f => f.Id)));

        if (featProficiencyError is not null)
        {
            return featProficiencyError;
        }

        // The race's skills were picked before the background was known
        var repeatedProficiency = ProficiencyPickCheck.FindRepeated(draft, race, subrace, background, @class);

        if (repeatedProficiency is not null)
        {
            return repeatedProficiency;
        }

        // A style the class owes and was never picked, or one from a step a draft skipped
        var invalidStyle = FightingStyleChoice.FindInvalidPick(
            draft.FightingStyleIds,
            sources.Feats.ToDictionary(f => f.Id),
            draft.SelectedFeatIds,
            draft,
            @class,
            sources.Subclass,
            level);

        if (invalidStyle is not null)
        {
            return invalidStyle;
        }

        // The spells step checks the same, but a draft whose level, class, subclass or scores
        // changed afterwards, or that never reached that step, still holds the old picks or none
        return FindMissingFeatChoice(draft, sources, raceName, level)
            ?? SpellChoice.FindInvalidPick(
                draft.SelectedSpellIds,
                sources.ChoosableSpells,
                @class.SpellAllowanceAt(level, sources.Subclass, finalAbilityScores),
                sources.Subclass,
                @class.Name,
                level)
            ?? BonusSpells.FindInvalidPick(
                draft,
                BonusSpells.RaceSlot(race, subrace),
                BonusSpells.FeatSlots(draft, background, sources.Feats.ToDictionary(f => f.Id)),
                draft.RaceSpellIds,
                draft.FeatSpells,
                sources.BonusSpellLists);
    }

    // A level-up adds its hit points, and every result kept is for a level the character has and
    // one its die rolls: an edit may have lowered the level, or a reseed changed the die
    private static Error? FindInvalidHitDieResult(CharacterDraft draft, string hitDie, int level)
    {
        if (draft.IsLevelUp && !draft.HitDieResults.ContainsKey(level))
        {
            return CharacterErrors.HitPointsRequired(level);
        }

        var dieSize = CharacterStatsCalculator.GetHitDieSize(hitDie);
        var invalid = draft.HitDieResults.Where(r => r.Key < 2 || r.Key > level || r.Value < 1 || r.Value > dieSize).ToList();

        return invalid.Count == 0 ? null : CharacterErrors.HitDieResultInvalid(invalid[0].Key, invalid[0].Value, dieSize);
    }

    // A Mist Wanderer's Dark Gift, or a Haunted One's in place of Survivor: the step checks it,
    // but a reseed may have moved the feat
    private static Error? FindInvalidBackgroundFeat(CharacterDraft draft, CharacterSources sources)
    {
        var background = sources.Background;

        if (background.PickableFeatCategory is not { } category)
        {
            return draft.BackgroundFeatId == null ? null : CharacterDraftErrors.BackgroundFeatNotChoosable(background.Name);
        }

        // Without a pick a background with an alternative keeps its own feat
        if (draft.BackgroundFeatId == null && background.FeatChoiceCategory == null)
        {
            return null;
        }

        var feat = sources.Feats.FirstOrDefault(f => f.Id == draft.BackgroundFeatId);

        if (feat == null)
        {
            return CharacterErrors.BackgroundFeatRequired(background.Name, category);
        }

        if (feat.Edition != draft.Edition)
        {
            return CharacterDraftErrors.EditionMismatch("feat", feat.Name, feat.Edition, draft.Edition);
        }

        return feat.Category == category ? null : CharacterDraftErrors.BackgroundFeatNotOfCategory(background.Name, feat.Name, category);
    }

    // The background's own feat comes with it, and the spells a subclass always has with that
    private static Error? FindChoiceFromBookNotAllowed(CharacterDraft draft, CharacterSources sources)
    {
        var chosen = new List<(string What, string Name, string Source)>
        {
            ("race", sources.Race.Name, sources.Race.Source),
            ("class", sources.Class.Name, sources.Class.Source),
            ("background", sources.Background.Name, sources.Background.Source),
        };

        if (sources.Subrace is { } subrace)
        {
            chosen.Add(("subrace", subrace.Name, subrace.Source));
        }

        if (sources.Subclass is { } subclass)
        {
            chosen.Add(("subclass", subclass.Name, subclass.Source));
        }

        var pickedFeatIds = draft.SelectedFeatIds.Concat(draft.FightingStyleIds).Append(draft.BackgroundFeatId).ToHashSet();
        chosen.AddRange(sources.Feats.Where(f => pickedFeatIds.Contains(f.Id)).Select(f => ("feat", f.Name, f.Source)));
        chosen.AddRange(sources.ChoosableSpells.Where(s => draft.SelectedSpellIds.Contains(s.Id)).Select(s => ("spell", s.Name, s.Source)));

        var (what, name, source) = chosen.Find(c => !draft.AllowsSource(c.Source));

        return what is null ? null : CharacterDraftErrors.SourceNotAllowed(what, name, source);
    }

    // The race's own feats come first among the feats (a 2024 human's Origin feat), then one for
    // each improvement not spent on ability points, and each feat's increase must be picked
    private static Error? FindMissingFeatChoice(CharacterDraft draft, CharacterSources sources, string raceName, int level)
    {
        var featsById = sources.Feats.ToDictionary(f => f.Id);
        var bonusFeats = sources.Race.BonusFeatsWith(sources.Subrace);
        var raceFeatIds = draft.SelectedFeatIds.Take(bonusFeats.Count).ToList();

        if (raceFeatIds.Count < bonusFeats.Count
            || raceFeatIds.Exists(id => !featsById.TryGetValue(id, out var feat) || !bonusFeats.Accepts(feat.Category)))
        {
            return CharacterErrors.BonusFeatsIncomplete(raceName, bonusFeats.Count, bonusFeats.Category);
        }

        var requiredAsis = sources.Class.AsiLevels.Count(l => l <= level);
        var spentAsis = draft.AbilityScoreImprovements.Count + draft.SelectedFeatIds.Count - bonusFeats.Count;

        if (spentAsis != requiredAsis)
        {
            return CharacterErrors.AbilityScoreImprovementsIncomplete(level, sources.Class.Name, requiredAsis, spentAsis);
        }

        for (var i = 0; i < draft.SelectedFeatIds.Count; i++)
        {
            var feat = featsById.GetValueOrDefault(draft.SelectedFeatIds[i]);
            var picked = draft.FeatAbilityIncreaseAt(i);

            // A feat raising nothing takes no pick; one raising a fixed ability needs none
            var resolves = feat?.AbilityIncrease is { } increase
                ? increase.Resolve(picked) != null
                : picked.Values.All(amount => amount == 0);

            if (!resolves)
            {
                return CharacterErrors.FeatAbilityIncreaseIncomplete(feat?.Name ?? draft.SelectedFeatIds[i]);
            }
        }

        // A feat the reference data no longer has, after a reseed, would otherwise finalize unseen
        var unknownFeatIds = draft.SelectedFeatIds.Where(id => !featsById.ContainsKey(id)).Distinct().ToList();

        if (unknownFeatIds.Count != 0)
        {
            return CharacterDraftErrors.InvalidFeatIds(unknownFeatIds);
        }

        // The improvements step checked these when they were picked, but the level, race, class
        // or scores may have changed since. The background's own feat comes with it and is not checked.
        return FeatPrerequisiteCheck.FindUnmet(
            draft.SelectedFeatIds.ConvertAll(id => featsById[id]),
            bonusFeats.Count,
            draft.AbilityScoreImprovements,
            draft,
            sources.Race,
            sources.Subrace,
            sources.Class,
            sources.Subclass,
            level);
    }
}
