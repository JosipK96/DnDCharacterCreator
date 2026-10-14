using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Whether a character may take the feats it picked. The improvements step and finalize both check
/// through here: lowering the level, changing the race or class, or lowering a score after the
/// feats were picked used to leave an Epic Boon at level 4 or Elven Accuracy on a dwarf. A feat
/// for one subrace (Wood Elf Magic) or one size (Squat Nimbleness) is checked against those too.
/// </summary>
public static class FeatPrerequisiteCheck
{
    /// <summary>
    /// The first picked feat whose prerequisites the draft does not meet, as an error naming it
    /// and what it asks for, or null when every one is met.
    /// </summary>
    public static Error? FindUnmet(
        IEnumerable<FeatReadModel> pickedFeats,
        CharacterDraft draft,
        Race? race,
        Subrace? subrace,
        Class @class,
        Subclass? subclass,
        int level)
    {
        var scores = ScoresBeforeImprovements(draft, race, subrace);
        var featureNames = CharacterFeatures
            .ClassFeaturesFor(@class, subclass, level)
            .Select(f => f.Name)
            .ToList();

        var raceSizes = race?.SizeOptions() ?? [];

        foreach (var feat in pickedFeats.DistinctBy(f => f.Id))
        {
            var unmet = feat.Prerequisites.FindUnmet(
                level, draft.RaceId, draft.SubraceId, raceSizes, draft.ClassId, scores, featureNames);

            if (unmet is not null)
            {
                return CharacterDraftErrors.FeatPrerequisiteNotMet(feat.Name, unmet);
            }
        }

        return null;
    }

    // A prerequisite counts the score as it stands, racial bonuses, the race's picks and a 2024
    // background's increases included: a half-orc's base Strength of 12 is 14 by the time a feat
    // asking for 13 comes up. This used to check the base scores alone.
    private static Dictionary<string, int> ScoresBeforeImprovements(CharacterDraft draft, Race? race, Subrace? subrace)
    {
        var scores = new Dictionary<string, int>();

        foreach (var (key, value) in draft.BaseAbilityScores)
        {
            if (AbilityScore.TryNormalize(key, out var abilityId))
            {
                scores[abilityId] = value;
            }
        }

        var bonuses = (race?.AbilityBonuses.Select(b => KeyValuePair.Create(b.AbilityScoreId, b.Bonus)) ?? [])
            .Concat(subrace?.AbilityBonuses.Select(b => KeyValuePair.Create(b.AbilityScoreId, b.Bonus)) ?? [])
            .Concat(draft.RaceAbilityIncreases)
            .Concat(draft.BackgroundAbilityIncreases);

        foreach (var (key, bonus) in bonuses)
        {
            var abilityId = AbilityScore.Normalize(key);
            scores[abilityId] = scores.GetValueOrDefault(abilityId) + bonus;
        }

        return scores;
    }
}
