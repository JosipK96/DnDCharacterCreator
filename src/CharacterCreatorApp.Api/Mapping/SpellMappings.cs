using CharacterCreatorApp.Application.ReadModels.Spells;
using CharacterCreatorApp.Contracts.Responses.Spells;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the spell read models into the responses the client reads.</summary>
public static class SpellMappings
{
    // CastingTime, Range, Components and Duration are the read model's own sentences, built from
    // the stored parts, so every page shows a spell the same way
    public static SpellApiResponse ToResponse(this SpellReadModel spell) => new(
        Id: spell.Id,
        Name: spell.Name,
        Source: spell.Source,
        Level: spell.Level,
        School: spell.School,
        CastingTime: spell.CastingTime,
        Range: spell.Range,
        Components: spell.Components,
        Duration: spell.Duration,
        Description: spell.Description,
        IsRitual: spell.IsRitual,
        IsConcentration: spell.IsConcentration,
        ClassIds: spell.ClassIds);

    public static SpellAllowanceApiResponse ToResponse(this SpellAllowanceReadModel allowance) => new(
        CastsSpells: allowance.CastsSpells,
        Cantrips: allowance.Cantrips,
        Spells: allowance.Spells,
        MaxSpellLevel: allowance.MaxSpellLevel,
        SpellsFollowScores: allowance.SpellsFollowScores,
        AlwaysPreparedSpellIds: allowance.AlwaysPreparedSpellIds);

    public static BonusSpellChoiceApiResponse ToResponse(this BonusSpellChoiceReadModel choice) => new(
        FeatId: choice.FeatId,
        Name: choice.Name,
        Cantrips: choice.Cantrips,
        FirstLevelSpells: choice.FirstLevelSpells,
        Lists: choice.Lists.Select(list => list.ToResponse()).ToList(),
        AbilityOptions: choice.AbilityOptions);

    public static BonusSpellListApiResponse ToResponse(this BonusSpellListReadModel list) => new(
        ClassId: list.ClassId,
        ClassName: list.ClassName,
        Spells: list.Spells.Select(spell => spell.ToResponse()).ToList());
}
