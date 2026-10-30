using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>What a sheet lists beside the numbers, with every id resolved to its entry.</summary>
public sealed record CharacterSheetContent(
    List<ProficiencyReadModel> Proficiencies,
    List<FeatureReadModel> Features,
    List<SpellReadModel> Spells,
    List<LanguageReadModel> Languages,
    List<CharacterFeatReadModel> Feats);
