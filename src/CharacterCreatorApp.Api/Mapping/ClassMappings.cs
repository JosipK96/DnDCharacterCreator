using CharacterCreatorApp.Application.ReadModels.Classes;
using CharacterCreatorApp.Contracts.Responses.Classes;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the class, subclass and class feature read models into the responses the client reads.</summary>
public static class ClassMappings
{
    public static ClassApiResponse ToResponse(this ClassReadModel @class) => new(
        Id: @class.Id,
        Name: @class.Name,
        Edition: @class.Edition,
        Source: @class.Source,
        HitDie: @class.HitDie,
        SubclassLevel: @class.SubclassLevel,
        AsiLevels: @class.AsiLevels,
        ClassProficiencies: @class.ClassProficiencies.Select(proficiency => proficiency.ToResponse()).ToList(),
        ProficiencyChoice: @class.ProficiencyChoice?.ToResponse(),
        Spellcasting: @class.Spellcasting?.ToResponse(),
        SpellProgression: @class.SpellProgression.Select(row => row.ToResponse()).ToList(),
        SavingThrowAbilityIds: @class.SavingThrowAbilityIds);

    public static SubclassApiResponse ToResponse(this SubclassReadModel subclass) => new(
        Id: subclass.Id,
        Name: subclass.Name,
        ClassId: subclass.ClassId,
        Source: subclass.Source,
        SpellListClassId: subclass.SpellListClassId,
        SpellProgression: subclass.SpellProgression.Select(row => row.ToResponse()).ToList());

    public static SpellcastingApiResponse ToResponse(this SpellcastingReadModel spellcasting) => new(
        SpellcastingAbility: spellcasting.SpellcastingAbility,
        IsPreparedCaster: spellcasting.IsPreparedCaster);

    public static SpellProgressionApiResponse ToResponse(this SpellProgressionReadModel row) => new(
        CharacterLevel: row.CharacterLevel,
        SpellsKnown: row.SpellsKnown,
        CantripsKnown: row.CantripsKnown,
        MaxSpellLevel: row.MaxSpellLevel,
        SpellcastingAbility: row.SpellcastingAbility);

    public static FeatureApiResponse ToResponse(this FeatureReadModel feature) =>
        new(Id: feature.Id, Name: feature.Name, Description: feature.Description);

    public static FightingStylesApiResponse ToResponse(this FightingStylesReadModel styles) => new(
        Owed: styles.Owed,
        Options: styles.Options.Select(style => style.ToResponse()).ToList());
}
