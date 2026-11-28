using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses.Characters;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>
/// Turns the character and draft read models into the responses the client reads, and the
/// character list's filter from the wire into the Application's own.
/// </summary>
public static class CharacterMappings
{
    public static DraftChoicesOwedApiResponse ToResponse(this DraftChoicesOwedReadModel owed) => new(
        Improvements: owed.Improvements,
        ImprovementLevels: owed.ImprovementLevels,
        BonusFeats: owed.BonusFeats,
        BonusFeatCategory: owed.BonusFeatCategory,
        BackgroundFeatId: owed.BackgroundFeatId,
        Languages: owed.Languages,
        KnownLanguageIds: owed.KnownLanguageIds,
        HitDieSize: owed.HitDieSize,
        HitDieFixedValue: owed.HitDieFixedValue);

    public static CharacterDetailsApiResponse ToResponse(this CharacterDetailsReadModel character) => new(
        Id: character.Id,
        Name: character.Name,
        Level: character.Level,
        Edition: character.Edition,
        RaceId: character.RaceId,
        SubraceId: character.SubraceId,
        ClassId: character.ClassId,
        SubclassId: character.SubclassId,
        BackgroundId: character.BackgroundId,
        RaceName: character.RaceName,
        SubraceName: character.SubraceName,
        ClassName: character.ClassName,
        SubclassName: character.SubclassName,
        BackgroundName: character.BackgroundName,
        Size: character.Size,
        AbilityScores: character.AbilityScores,
        Proficiencies: character.Proficiencies.Select(proficiency => proficiency.ToResponse()).ToList(),
        LanguageIds: character.LanguageIds,
        Languages: character.Languages.Select(language => language.ToResponse()).ToList(),
        Features: character.Features.Select(feature => feature.ToResponse()).ToList(),
        Spells: character.Spells.Select(spell => spell.ToResponse()).ToList(),
        Feats: character.Feats.Select(feat => feat.ToResponse()).ToList(),
        Stats: character.Stats?.ToResponse());

    // The review step shows what finalize would build, so it reads the same pieces as the sheet
    public static CharacterPreviewApiResponse ToResponse(this CharacterPreviewReadModel preview) => new(
        Name: preview.Name,
        Level: preview.Level,
        Edition: preview.Edition,
        RaceName: preview.RaceName,
        SubraceName: preview.SubraceName,
        ClassName: preview.ClassName,
        SubclassName: preview.SubclassName,
        BackgroundName: preview.BackgroundName,
        Size: preview.Size,
        Proficiencies: preview.Proficiencies.Select(proficiency => proficiency.ToResponse()).ToList(),
        Languages: preview.Languages.Select(language => language.ToResponse()).ToList(),
        Features: preview.Features.Select(feature => feature.ToResponse()).ToList(),
        Spells: preview.Spells.Select(spell => spell.ToResponse()).ToList(),
        Feats: preview.Feats.Select(feat => feat.ToResponse()).ToList(),
        Stats: preview.Stats.ToResponse(),
        BlockingIssue: preview.BlockingIssue);

    public static CharacterFeatApiResponse ToResponse(this CharacterFeatReadModel feat) => new(
        Feat: feat.Feat.ToResponse(),
        Spells: feat.Spells.Select(spell => spell.ToResponse()).ToList(),
        SpellcastingAbilityId: feat.SpellcastingAbilityId);

    public static CharacterSummaryApiResponse ToResponse(this CharacterReadModel character) => new()
    {
        Id = character.Id,
        Name = character.Name,
        ClassName = character.ClassName,
        SubclassName = character.SubclassName,
        RaceName = character.RaceName,
        SubraceName = character.SubraceName,
        Level = character.Level,
        Edition = character.Edition,
        BackgroundName = character.BackgroundName,
        CreatedAt = character.CreatedAt,
    };

    public static FilterResponse<CharacterSummaryApiResponse> ToResponse(this PagedResult<CharacterReadModel> page) => new(
        items: page.Items.Select(character => character.ToResponse()),
        totalCount: page.TotalCount,
        pageIndex: page.PageIndex,
        pageSize: page.PageSize);

    /// <summary>The list's filter as the Application takes it; no body at all means the first page.</summary>
    public static FilterOptions<FilterCharacterSearchParams> ToFilterOptions(this FilterRequest<GetCharactersApiRequest>? request)
    {
        if (request == null)
        {
            return FilterOptions<FilterCharacterSearchParams>.Default;
        }

        return new FilterOptions<FilterCharacterSearchParams>
        {
            SearchParams = request.SearchParams == null ? null : new FilterCharacterSearchParams
            {
                Name = request.SearchParams.Name,
                ClassId = request.SearchParams.ClassId,
                MinLevel = request.SearchParams.MinLevel,
                MaxLevel = request.SearchParams.MaxLevel,
            },
            Sorting = request.Sorting == null ? null : new SortOptions
            {
                SortColumn = request.Sorting.SortColumn,
                SortDirection = request.Sorting.SortDirection,
            },
            Pagination = request.Pagination == null ? null : new PageOptions
            {
                PageIndex = request.Pagination.PageIndex,
                PageSize = request.Pagination.PageSize,
            },
        };
    }

    public static CharacterDraftDetailsApiResponse ToResponse(this CharacterDraftDetailsReadModel draft) => new()
    {
        Id = draft.Id,
        Name = draft.Name,
        Level = draft.Level,
        Edition = draft.Edition,
        AllowedSources = draft.AllowedSources,
        RaceId = draft.RaceId,
        SubraceId = draft.SubraceId,
        Size = draft.Size,
        RaceSpellIds = draft.RaceSpellIds,
        RaceSpellAbilityId = draft.RaceSpellAbilityId,
        RaceAbilityIncreases = draft.RaceAbilityIncreases,
        ClassId = draft.ClassId,
        SubclassId = draft.SubclassId,
        BackgroundId = draft.BackgroundId,
        BackgroundFeatId = draft.BackgroundFeatId,
        BackgroundAbilityIncreases = draft.BackgroundAbilityIncreases,
        BaseAbilityScoreMethod = draft.BaseAbilityScoreMethod,
        BaseAbilityScores = draft.BaseAbilityScores,
        SelectedClassProficiencyIds = draft.SelectedClassProficiencyIds,
        SelectedRaceSkillIds = draft.SelectedRaceSkillIds,
        ToolPicks = draft.ToolPicks,
        SelectedLanguageIds = draft.SelectedLanguageIds,
        SelectedFeatIds = draft.SelectedFeatIds,
        FeatAbilityIncreases = draft.FeatAbilityIncreases,
        FeatProficiencyPicks = draft.FeatProficiencyPicks.Select(pick => pick.ToResponse()).ToList(),
        FightingStyleIds = draft.FightingStyleIds,
        SelectedSpellIds = draft.SelectedSpellIds,
        FeatSpells = draft.FeatSpells.Select(pick => pick.ToResponse()).ToList(),
        AbilityScoreImprovements = draft.AbilityScoreImprovements,
        IsCompleted = draft.IsCompleted,
        CharacterId = draft.CharacterId,
        ChoicesNeeded = draft.ChoicesNeeded,
        LevelUpChoices = draft.LevelUpChoices,
        HitDieResults = draft.HitDieResults,
        HitDieLevels = draft.HitDieLevels,
    };

    public static CharacterStatsApiResponse ToResponse(this CharacterStats stats) => new(
        ProficiencyBonus: stats.ProficiencyBonus,
        MaxHitPoints: stats.MaxHitPoints,
        ArmorClass: stats.ArmorClass,
        Initiative: stats.Initiative,
        Speed: stats.Speed,
        HitDice: stats.HitDice,
        PassivePerception: stats.PassivePerception,
        Abilities: stats.Abilities.Select(a => new AbilityStatApiResponse(a.AbilityId, a.Score, a.Modifier)).ToList(),
        SavingThrows: stats.SavingThrows.Select(s => new SavingThrowStatApiResponse(s.AbilityId, s.Modifier, s.IsProficient)).ToList(),
        Skills: stats.Skills.Select(s => new SkillStatApiResponse(s.SkillId, s.Name, s.AbilityId, s.Modifier, s.IsProficient)).ToList(),
        SpellSlots: stats.SpellSlots.Select(s => new SpellSlotStatApiResponse(s.SpellLevel, s.Slots)).ToList(),
        PactMagic: stats.PactMagic == null ? null : new PactMagicStatApiResponse(stats.PactMagic.SlotLevel, stats.PactMagic.Slots),
        Spellcasting: stats.Spellcasting.Select(s => new SpellcastingStatApiResponse(s.Label, s.AbilityId, s.SaveDc, s.AttackBonus)).ToList(),
        Attacks: stats.Attacks.Select(a => new SpellAttackStatApiResponse(a.Name, a.AttackBonus, a.SaveDc, a.SaveAbilityId, a.Damage, a.DamageType)).ToList());
}
