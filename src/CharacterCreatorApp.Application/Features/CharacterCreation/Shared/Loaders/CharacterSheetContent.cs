using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Application.ReadModels.Classes;
using CharacterCreatorApp.Application.ReadModels.Languages;
using CharacterCreatorApp.Application.ReadModels.Proficiencies;
using CharacterCreatorApp.Application.ReadModels.Spells;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;

/// <summary>What a sheet lists beside the numbers, with every id resolved to its entry.</summary>
public sealed record CharacterSheetContent(
    List<ProficiencyReadModel> Proficiencies,
    List<FeatureReadModel> Features,
    List<SpellReadModel> Spells,
    List<LanguageReadModel> Languages,
    List<CharacterFeatReadModel> Feats);
