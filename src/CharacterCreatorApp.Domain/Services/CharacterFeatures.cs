using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// The features a character has: its class's and its subclass's up to its level, and its
/// background's. Worked out from the rules data whenever a sheet is shown instead of being stored
/// with the character, so a sheet never lists a feature the data has since renamed or dropped,
/// and the review step and the saved sheet cannot disagree.
/// </summary>
public static class CharacterFeatures
{
    /// <summary>The class and subclass features a character of this level has.</summary>
    public static IEnumerable<Feature> ClassFeaturesFor(Class @class, Subclass? subclass, int level) =>
        @class.Features
            .Where(f => f.Level <= level && string.IsNullOrEmpty(f.SubclassId))
            .Concat(subclass?.Features.Where(f => f.Level <= level) ?? []);

    /// <summary>
    /// The ids of those features and the background's; a 2024 background has none, its origin
    /// feat taking the place of a feature.
    /// </summary>
    public static List<string> IdsFor(Class @class, Subclass? subclass, Background background, int level) =>
        ClassFeaturesFor(@class, subclass, level)
            .Select(f => f.Id)
            .Concat(background.FeatureId is { } featureId ? [featureId] : [])
            .Distinct()
            .ToList();
}
