using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Checks the choices whose required count only becomes known once the race, class and
/// background are loaded. A draft can reach finalize with a step skipped entirely, so each
/// count is compared against what the character is actually entitled to.
/// </summary>
public static class DraftCompletenessChecker
{
    /// <summary>Returns null when every choice the character is owed has been made.</summary>
    public static Error? FindMissingChoice(CharacterDraft draft, CharacterSources sources)
    {
        var (race, subrace, @class, background) = (sources.Race, sources.Subrace, sources.Class, sources.Background);
        var raceName = subrace?.Name ?? race.Name;
        var level = draft.Level!.Value;

        // A dwarf is a hill or a mountain dwarf, a 2024 elf has a lineage; a 2014 human with
        // neither the standard nor the variant choice would have no increases at all
        if (subrace == null && race.Subraces.Count != 0)
        {
            return CharacterErrors.SubraceRequired(race.Name, race.Subraces.Select(s => s.Name));
        }

        if (level >= @class.SubclassLevel && string.IsNullOrWhiteSpace(draft.SubclassId))
        {
            return CharacterErrors.SubclassRequired(level, @class.Name, @class.SubclassLevel);
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

        // The race's skills were picked before the background was known
        var repeatedProficiency = ProficiencyPickCheck.FindRepeated(draft, race, subrace, background, @class);

        if (repeatedProficiency is not null)
        {
            return repeatedProficiency;
        }

        // The spells step checks the same, but a draft whose level, class or subclass changed
        // afterwards, or that never reached that step, still holds the old picks or none
        return FindMissingFeatChoice(draft, sources, raceName, level)
            ?? SpellChoice.FindInvalidPick(
                draft.SelectedSpellIds,
                sources.ChoosableSpells,
                @class.SpellAllowanceAt(level, sources.Subclass),
                @class.Name,
                level);
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
            draft.SelectedFeatIds.Select(id => featsById[id]),
            draft,
            sources.Race,
            sources.Subrace,
            sources.Class,
            sources.Subclass,
            level);
    }
}
