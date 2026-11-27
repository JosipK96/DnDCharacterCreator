using CharacterCreatorApp.Application.ReadModels.Backgrounds;
using CharacterCreatorApp.Contracts.Responses.Backgrounds;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the background read models into the responses the client reads.</summary>
public static class BackgroundMappings
{
    public static BackgroundApiResponse ToResponse(this BackgroundReadModel background) => new(
        Id: background.Id,
        Name: background.Name,
        Edition: background.Edition,
        Source: background.Source,
        Languages: background.Languages,
        LanguageChoiceCount: background.LanguageChoiceCount,
        Proficiencies: background.Proficiencies.Select(proficiency => proficiency.ToResponse()).ToList(),
        AbilityScoreOptions: background.AbilityScoreOptions,
        Feat: background.Feat?.ToResponse(),
        Feature: background.Feature?.ToResponse(),
        FeatChoiceCategory: background.FeatChoiceCategory,
        FeatAlternativeCategory: background.FeatAlternativeCategory);

    public static BackgroundProficiencyApiResponse ToResponse(this BackgroundProficiencyReadModel proficiency) =>
        new(Id: proficiency.Id, Name: proficiency.Name, Type: proficiency.Type);

    public static BackgroundFeatApiResponse ToResponse(this BackgroundFeatReadModel feat) =>
        new(Id: feat.Id, Name: feat.Name, IsRepeatable: feat.IsRepeatable);

    public static BackgroundFeatureApiResponse ToResponse(this BackgroundFeatureReadModel feature) =>
        new(Id: feature.Id, Name: feature.Name, Description: feature.Description);
}
