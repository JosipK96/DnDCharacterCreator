using CharacterCreatorApp.Application.ReadModels.Races;
using CharacterCreatorApp.Contracts.Responses.Races;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the race read models the Application returns into the responses the client reads.</summary>
public static class RaceMappings
{
    public static RaceApiResponse ToResponse(this RaceReadModel race) => new(
        Id: race.Id,
        Name: race.Name,
        Edition: race.Edition,
        Source: race.Source,
        Size: race.Size,
        Speed: race.Speed,
        SkillChoiceCount: race.SkillChoiceCount,
        SkillChoiceOptions: race.SkillChoiceOptions,
        LanguageChoiceCount: race.LanguageChoiceCount,
        AbilityChoiceCount: race.AbilityChoiceCount,
        AbilityChoiceOptions: race.AbilityChoiceOptions,
        FeatChoiceCount: race.FeatChoiceCount,
        FeatChoiceCategory: race.FeatChoiceCategory,
        AbilityScoreBonuses: race.AbilityScoreBonuses.Select(bonus => bonus.ToResponse()).ToList(),
        Languages: race.Languages,
        Traits: race.Traits.Select(trait => trait.ToResponse()).ToList(),
        Subraces: race.Subraces.Select(subrace => subrace.ToResponse()).ToList(),
        FixedProficiencies: race.FixedProficiencies.Select(proficiency => proficiency.ToResponse()).ToList(),
        AbilityChoiceMaxPerAbility: race.AbilityChoiceMaxPerAbility,
        AbilityChoiceMinPerAbility: race.AbilityChoiceMinPerAbility);

    public static SubraceApiResponse ToResponse(this SubraceReadModel subrace) => new(
        Id: subrace.Id,
        Name: subrace.Name,
        RaceId: subrace.RaceId,
        Source: subrace.Source,
        Speed: subrace.Speed,
        LanguageChoiceCount: subrace.LanguageChoiceCount,
        AbilityChoiceCount: subrace.AbilityChoiceCount,
        AbilityChoiceOptions: subrace.AbilityChoiceOptions,
        SkillChoiceCount: subrace.SkillChoiceCount,
        SkillChoiceOptions: subrace.SkillChoiceOptions,
        FeatChoiceCount: subrace.FeatChoiceCount,
        FeatChoiceCategory: subrace.FeatChoiceCategory,
        AbilityScoreBonuses: subrace.AbilityScoreBonuses.Select(bonus => bonus.ToResponse()).ToList(),
        Traits: subrace.Traits.Select(trait => trait.ToResponse()).ToList(),
        AbilityChoiceMaxPerAbility: subrace.AbilityChoiceMaxPerAbility,
        AbilityChoiceMinPerAbility: subrace.AbilityChoiceMinPerAbility);

    // The client gets the ability's name ("Strength"), not the value object
    public static AbilityScoreBonusApiResponse ToResponse(this AbilityScoreBonusReadModel bonus) =>
        new(AbilityScore: bonus.AbilityScore.Name, Bonus: bonus.Bonus);

    public static TraitApiResponse ToResponse(this TraitReadModel trait) =>
        new(Id: trait.Id, Name: trait.Name, Description: trait.Description);
}
