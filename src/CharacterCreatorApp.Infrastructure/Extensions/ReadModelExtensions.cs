using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.DomainModels.Spells;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Infrastructure.Extensions;

public static class ReadModelExtensions
{
    public static ClassReadModel ToReadModel(this Class entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Edition = entity.Edition,
        HitDie = entity.HitDie,
        SubclassLevel = entity.SubclassLevel,
        AsiLevels = entity.AsiLevels,
        SavingThrowAbilityIds = entity.SavingThrowAbilityIds,
        ClassProficiencies = entity.ClassProficiencies.Select(cp => cp.Proficiency.ToReadModel()).ToList(),
        ProficiencyChoice = entity.ProficiencyChoice?.ToReadModel(),
        Spellcasting = entity.Spellcasting?.ToReadModel(),
        SpellProgression = entity.SpellProgression.Select(sp => sp.ToReadModel()).ToList()
    };

    public static SpellcastingReadModel ToReadModel(this Spellcasting s) => new()
    {
        SpellcastingAbility = s.SpellcastingAbility,
        IsPreparedCaster = s.IsPreparedCaster
    };

    public static ProficiencyChoiceReadModel ToReadModel(this ProficiencyChoice proficiencyChoice) => new()
    {
        Type = proficiencyChoice.Type,
        Count = proficiencyChoice.Count,
        Options = proficiencyChoice.Options.Select(o => o.Proficiency.ToReadModel()).ToList()
    };

    public static RaceReadModel ToReadModel(this Race race) => new()
    {
        Id = race.Id,
        Name = race.Name,
        Edition = race.Edition,
        Size = race.Size,
        Speed = race.Speed,
        SkillChoiceCount = race.SkillChoiceCount,
        SkillChoiceOptions = race.SkillChoiceOptions,
        LanguageChoiceCount = race.LanguageChoiceCount,
        AbilityChoiceCount = race.AbilityChoiceCount,
        AbilityChoiceOptions = race.AbilityChoiceOptions,
        FeatChoiceCount = race.FeatChoiceCount,
        FeatChoiceCategory = race.FeatChoiceCategory,
        AbilityScoreBonuses = race.AbilityBonuses.Select(asb => asb.ToReadModel()).ToList(),
        Languages = race.RaceLanguages.Select(rl => rl.Language).ToList(),
        Traits = race.Traits.Select(t => t.ToReadModel()).ToList(),
        Subraces = race.Subraces.Select(sr => sr.ToReadModel()).ToList(),
        FixedProficiencies = race.RaceProficiencies.Select(rp => rp.Proficiency.ToReadModel()).ToList()
    };

    public static SubraceReadModel ToReadModel(this Subrace subrace) => new()
    {
        Id = subrace.Id,
        Name = subrace.Name,
        RaceId = subrace.RaceId,
        Speed = subrace.Speed,
        LanguageChoiceCount = subrace.LanguageChoiceCount,
        AbilityChoiceCount = subrace.AbilityChoiceCount,
        AbilityChoiceOptions = subrace.AbilityChoiceOptions,
        SkillChoiceCount = subrace.SkillChoiceCount,
        SkillChoiceOptions = subrace.SkillChoiceOptions,
        FeatChoiceCount = subrace.FeatChoiceCount,
        FeatChoiceCategory = subrace.FeatChoiceCategory,
        AbilityScoreBonuses = subrace.AbilityBonuses.Select(asb => asb.ToReadModel()).ToList(),
        Traits = subrace.Traits.Select(t => t.ToReadModel()).ToList()
    };

    public static TraitReadModel ToReadModel(this RaceTrait trait) => new()
    {
        Id = trait.Id,
        Name = trait.Name,
        Description = trait.Description,
        ProficiencyIds = []
    };

    public static TraitReadModel ToReadModel(this SubraceTrait trait) => new()
    {
        Id = trait.Id,
        Name = trait.Name,
        Description = trait.Description,
        ProficiencyIds = []
    };

    public static SubclassReadModel ToReadModel(this Subclass subclass) => new()
    {
        Id = subclass.Id,
        Name = subclass.Name,
        ClassId = subclass.ClassId,
        SpellListClassId = subclass.SpellListClassId,
        SpellProgression = subclass.SpellProgression.Select(sp => sp.ToReadModel()).ToList()
    };

    public static SpellProgressionReadModel ToReadModel(this ClassSpellProgression sp) => new()
    {
        CharacterLevel = sp.CharacterLevel,
        SpellsKnown = sp.SpellsKnown,
        CantripsKnown = sp.CantripsKnown,
        MaxSpellLevel = sp.MaxSpellLevel,
        SpellcastingAbility = null
    };

    public static SpellProgressionReadModel ToReadModel(this SubclassSpellProgression sp) => new()
    {
        CharacterLevel = sp.CharacterLevel,
        SpellsKnown = sp.SpellsKnown,
        CantripsKnown = sp.CantripsKnown,
        MaxSpellLevel = sp.MaxSpellLevel,
        SpellcastingAbility = sp.SpellcastingAbility
    };

    public static FeatureReadModel ToReadModel(this Feature feature) => new()
    {
        Id = feature.Id,
        Name = feature.Name,
        Description = feature.Description,
        Level = feature.Level,
        ClassId = feature.ClassId,
        SubclassId = feature.SubclassId
    };

    public static BackgroundReadModel ToReadModel(this Background background)
    {
        return new BackgroundReadModel
        {
            Id = background.Id,
            Name = background.Name,
            Edition = background.Edition,
            AbilityScoreOptions = background.AbilityScoreOptions,
            Feat = background.Feat is { } feat
                ? new BackgroundFeatReadModel { Id = feat.Id, Name = feat.Name, IsRepeatable = feat.IsRepeatable }
                : null,

            LanguageChoiceCount = background.Languages.Count(l => l.Equals("any", StringComparison.OrdinalIgnoreCase)),

            Languages = background.Languages
                .Where(l => !l.Equals("any", StringComparison.OrdinalIgnoreCase))
                .Select(MapLanguageNameToStandard)
                .Where(lang => lang != null)
                .Cast<BackgroundLanguageReadModel>()
                .ToList(),

            Proficiencies = background.BackgroundProficiencies
                .Select(bp => new BackgroundProficiencyReadModel
                {
                    Id = bp.ProficiencyId,
                    Name = bp.Proficiency.Name,
                    Type = bp.Proficiency.Type
                })
                .ToList(),

            Feature = background.Feature is { } feature
                ? new BackgroundFeatureReadModel { Id = feature.Id, Name = feature.Name, Description = feature.Description }
                : null
        };
    }

    private static BackgroundLanguageReadModel? MapLanguageNameToStandard(string languageName)
    {
        // Map background language names to PHB standard language IDs
        var languageMap = new Dictionary<string, (string Id, string Name)>(StringComparer.OrdinalIgnoreCase)
        {
            // Standard Languages (PHB)
            ["Common"] = ("common", "Common"),
            ["Dwarvish"] = ("dwarvish", "Dwarvish"),
            ["Elvish"] = ("elvish", "Elvish"),
            ["Giant"] = ("giant", "Giant"),
            ["Gnomish"] = ("gnomish", "Gnomish"),
            ["Goblin"] = ("goblin", "Goblin"),
            ["Halfling"] = ("halfling", "Halfling"),
            ["Orc"] = ("orc", "Orc"),

            // Exotic Languages (PHB)
            ["Abyssal"] = ("abyssal", "Abyssal"),
            ["Celestial"] = ("celestial", "Celestial"),
            ["Draconic"] = ("draconic", "Draconic"),
            ["Deep Speech"] = ("deep-speech", "Deep Speech"),
            ["Infernal"] = ("infernal", "Infernal"),
            ["Primordial"] = ("primordial", "Primordial"),
            ["Sylvan"] = ("sylvan", "Sylvan"),
            ["Undercommon"] = ("undercommon", "Undercommon")
        };

        if (languageMap.TryGetValue(languageName, out var mapped))
        {
            return new BackgroundLanguageReadModel
            {
                Id = mapped.Id,
                Name = mapped.Name
            };
        }

        // Fallback for unknown languages
        return new BackgroundLanguageReadModel
        {
            Id = languageName.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal),
            Name = languageName
        };
    }

    public static ProficiencyReadModel ToReadModel(this Proficiency p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Type = p.Type
    };

    public static AbilityScoreBonusReadModel ToReadModel(this RaceAbilityBonus abilityScoreBonus) => new()
    {
        AbilityScore = AbilityScore.FromId(abilityScoreBonus.AbilityScoreId),
        Bonus = abilityScoreBonus.Bonus
    };

    public static AbilityScoreBonusReadModel ToReadModel(this SubraceAbilityBonus abilityScoreBonus) => new()
    {
        AbilityScore = AbilityScore.FromId(abilityScoreBonus.AbilityScoreId),
        Bonus = abilityScoreBonus.Bonus
    };

    public static SpellReadModel ToReadModel(this Spell spell) => new()
    {
        Id = spell.Id,
        Name = spell.Name,
        Level = spell.Level,
        School = spell.School,
        CastingTimeNumber = spell.CastingTimeNumber,
        CastingTimeUnit = spell.CastingTimeUnit,
        RangeType = spell.RangeType,
        RangeDistanceType = spell.RangeDistanceType,
        RangeDistanceAmount = spell.RangeDistanceAmount,
        HasVerbalComponent = spell.HasVerbalComponent,
        HasSomaticComponent = spell.HasSomaticComponent,
        HasMaterialComponent = spell.HasMaterialComponent,
        Material = spell.Material,
        DurationType = spell.DurationType,
        DurationAmount = spell.DurationAmount,
        DurationUnit = spell.DurationUnit,
        IsConcentration = spell.IsConcentration,
        IsRitual = spell.IsRitual,
        Description = spell.Description,
        ClassIds = spell.SpellClasses.Select(sc => sc.ClassId).ToList()
    };

    public static FeatReadModel ToReadModel(this Feat feat)
    {
        return new FeatReadModel
        {
            Id = feat.Id,
            Name = feat.Name,
            Description = feat.Description,
            Edition = feat.Edition,
            Category = feat.Category,
            Prerequisites = feat.Prerequisites,
            IsRepeatable = feat.IsRepeatable,
            AbilityIncrease = feat.AbilityIncrease,
            GrantedProficiencyIds = feat.GrantedProficiencyIds?.AsReadOnly() ?? new List<string>().AsReadOnly(),
            GrantedLanguageIds = feat.GrantedLanguageIds?.AsReadOnly() ?? new List<string>().AsReadOnly(),
            GrantedSpellIds = feat.GrantedSpellIds?.AsReadOnly() ?? new List<string>().AsReadOnly(),
            Choice = feat.Choice == null ? null : new FeatChoiceReadModel
            {
                Type = feat.Choice.Type,
                Count = feat.Choice.Count,
                Options = feat.Choice.Options.AsReadOnly()
            }
        };
    }

    public static CharacterReadModel ToReadModel(this Character c) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        Name = c.Name,
        Level = c.Level,
        Edition = c.Edition,
        ClassName = c.Class.Name,
        SubclassName = c.Subclass?.Name,
        RaceName = c.Race.Name,
        SubraceName = c.Subrace?.Name,
        BackgroundName = c.Background.Name,
        CreatedAt = c.CreatedAt.DateTime
    };

    public static CharacterDetailsReadModel ToDetailsReadModel(this Character character)
    {
        return new CharacterDetailsReadModel
        {
            Id = character.Id,
            Name = character.Name,
            Level = character.Level,
            Edition = character.Edition,

            // IDs
            RaceId = character.RaceId,
            SubraceId = character.SubraceId,
            ClassId = character.ClassId,
            SubclassId = character.SubclassId,
            BackgroundId = character.BackgroundId,

            // Names
            RaceName = character.Race.Name,
            SubraceName = character.Subrace?.Name,
            ClassName = character.Class.Name,
            SubclassName = character.Subclass?.Name,
            BackgroundName = character.Background.Name,

            // Data. GetCharacterDetailsQueryHandler looks up what these ids stand for; the
            // proficiency ids also decide which skills the sheet counts as proficient, and when
            // they were left off here every skill came out without the proficiency bonus.
            AbilityScores = character.AbilityScores,
            ProficiencyIds = character.ProficiencyIds,
            LanguageIds = character.LanguageIds,
            SpellIds = character.SpellIds,
            FeatIds = character.FeatIds,

            CreatedAt = character.CreatedAt.DateTime
        };
    }

    public static CharacterDraftDetailsReadModel ToDetailsReadModel(this CharacterDraft draft)
    {
        return new CharacterDraftDetailsReadModel
        {
            Id = draft.Id,
            Name = draft.Name,
            Level = draft.Level,
            Edition = draft.Edition,
            RaceId = draft.RaceId,
            SubraceId = draft.SubraceId,
            RaceAbilityIncreases = draft.RaceAbilityIncreases,
            ClassId = draft.ClassId,
            SubclassId = draft.SubclassId,
            BackgroundId = draft.BackgroundId,
            BackgroundAbilityIncreases = draft.BackgroundAbilityIncreases,
            BaseAbilityScores = draft.BaseAbilityScores,
            SelectedClassProficiencyIds = draft.SelectedClassProficiencyIds,
            SelectedRaceSkillIds = draft.SelectedRaceSkillIds,
            SelectedLanguageIds = draft.SelectedLanguageIds,
            SelectedFeatIds = draft.SelectedFeatIds,
            FeatAbilityIncreases = draft.FeatAbilityIncreases,
            SelectedSpellIds = draft.SelectedSpellIds,
            AbilityScoreImprovements = draft.AbilityScoreImprovements,
            IsCompleted = draft.IsCompleted
        };
    }
}