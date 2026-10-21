using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Turns a completed draft into a Character. Used both when finalizing and when previewing the
/// result beforehand, so the review step shows the character the player will actually get.
/// </summary>
public static class CharacterBuilder
{
    public static Character Build(
        Guid userId,
        CharacterDraft draft,
        CharacterSources sources,
        TimeProvider timeProvider)
    {
        var character = Character.Create(
            userId,
            draft.Name!,
            draft.Level!.Value,
            draft.Edition,
            draft.RaceId!,
            draft.SubraceId,
            draft.ClassId!,
            draft.SubclassId,
            draft.BackgroundId!,
            draft.BaseAbilityScores,
            timeProvider);

        character.ApplyRaceAbilityScoreBonuses(sources.Race.AbilityBonuses);

        if (sources.Subrace != null)
        {
            character.ApplySubraceAbilityScoreBonuses(sources.Subrace.AbilityBonuses);
        }

        // The half-elf's and the variant human's picks
        character.ApplyRaceAbilityIncreases(draft.RaceAbilityIncreases);

        // A 2024 character's increases come from its background instead of its species
        character.ApplyBackgroundAbilityIncreases(draft.BackgroundAbilityIncreases);
        character.ApplyAbilityScoreImprovements(draft.AbilityScoreImprovements);
        ApplyFeatAbilityIncreases(character, draft, sources.Feats);

        // The background's feat (a 2024 origin feat), the Fighting Styles, and those taken for improvements
        character.SetFeats(BackgroundFeatIds(sources.Background, draft).Concat(draft.FightingStyleIds).Concat(draft.SelectedFeatIds));
        character.SetFeatSpells(draft.FeatSpells);
        character.SetFeatSavingThrows(FeatSavingThrowAbilityIds(draft, sources.Feats));

        // A species that may be Small or Medium is the size picked for it
        character.SetSize(draft.Size ?? sources.Race.Size);

        character.SetLanguages(ResolveLanguageIds(draft, sources));

        character.SetProficiencies(sources.Race.RaceProficiencies
            .Select(p => p.ProficiencyId)
            .Concat(sources.Subrace?.SubraceProficiencies.Select(p => p.ProficiencyId) ?? [])
            .Concat(sources.Background.BackgroundProficiencies.Select(p => p.ProficiencyId))
            .Concat(sources.Class.ClassProficiencies.Select(p => p.ProficiencyId))
            .Concat(draft.SelectedClassProficiencyIds)
            .Concat(draft.SelectedRaceSkillIds)
            .Concat(draft.ToolPicks.SelectMany(pick => pick))
            .Concat(draft.FeatProficiencyPicks.SelectMany(pick => pick.ProficiencyIds))
            .Concat(sources.Feats.SelectMany(f => f.GrantedProficiencyIds))
            .Distinct());

        // What the player picked, for the class, the race (a high elf's cantrip) and feats (Magic
        // Initiate's), what feats grant, what the subclass always has prepared at this level (a
        // Life cleric's Bless and Cure Wounds), and what the race and subrace give by then (a
        // tiefling's Thaumaturgy, and Hellish Rebuke from 3)
        character.SetSpells(draft.SelectedSpellIds
            .Concat(draft.RaceSpellIds)
            .Concat(draft.FeatSpells.SelectMany(pick => pick.SpellIds))
            .Concat(sources.Feats.SelectMany(f => f.GrantedSpellIds))
            .Concat(sources.Subclass?.AlwaysPreparedSpellIdsAt(draft.Level!.Value) ?? [])
            .Concat(sources.Race.SpellIdsAt(draft.Level!.Value, sources.Subrace)));

        return character;
    }

    /// <summary>
    /// What each feat taken raises, as the player picked it (Athlete's Strength or Dexterity) or
    /// as it is fixed (Actor's Charisma), up to 20, or 30 for an Epic Boon. A pick the feat does not
    /// allow raises nothing; DraftCompletenessChecker reports it, and the preview shows the sheet
    /// without it.
    /// </summary>
    private static void ApplyFeatAbilityIncreases(Character character, CharacterDraft draft, IReadOnlyList<FeatReadModel> feats)
    {
        var featsById = feats.ToDictionary(f => f.Id);

        for (var i = 0; i < draft.SelectedFeatIds.Count; i++)
        {
            if (featsById.GetValueOrDefault(draft.SelectedFeatIds[i])?.AbilityIncrease is { } increase
                && increase.Resolve(draft.FeatAbilityIncreaseAt(i)) is { } raised)
            {
                character.ApplyFeatAbilityIncrease(raised, increase.Maximum);
            }
        }
    }

    /// <summary>
    /// The feat a background gives, as a list of none or one: its own, or the one the player
    /// picked for a background that lets them choose (Background.FeatChoiceCategory).
    /// </summary>
    public static IEnumerable<string> BackgroundFeatIds(Background background, CharacterDraft draft)
    {
        var featId = background.FeatChoiceCategory == null ? background.FeatId : draft.BackgroundFeatId;
        return featId is null ? [] : [featId];
    }

    // Resilient adds the saving throw of the ability it raises, so the save follows that pick
    private static IEnumerable<string> FeatSavingThrowAbilityIds(CharacterDraft draft, IReadOnlyList<FeatReadModel> feats)
    {
        var featsById = feats.ToDictionary(f => f.Id);

        for (var i = 0; i < draft.SelectedFeatIds.Count; i++)
        {
            if (featsById.GetValueOrDefault(draft.SelectedFeatIds[i]) is { Choice.Type: FeatChoice.SavingThrow, AbilityIncrease: { } increase }
                && increase.Resolve(draft.FeatAbilityIncreaseAt(i)) is { } raised)
            {
                foreach (var abilityId in raised.Keys)
                {
                    yield return abilityId;
                }
            }
        }
    }

    /// <summary>
    /// Every language the character ends up knowing: the race's and the background's own, the
    /// ones feats grant, and the ones the player picked for the background's "any" placeholders.
    /// </summary>
    private static List<string> ResolveLanguageIds(CharacterDraft draft, CharacterSources sources) =>
        LanguageResolver.ResolveIds(
            sources.Race.RaceLanguages.Select(x => x.Language)
                .Concat(sources.Background.Languages)
                .Concat(sources.Feats.SelectMany(f => f.GrantedLanguageIds))
                .Concat(draft.SelectedLanguageIds),
            sources.Languages);
}
