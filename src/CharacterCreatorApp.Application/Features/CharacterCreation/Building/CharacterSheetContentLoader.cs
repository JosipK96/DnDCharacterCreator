using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Resolves the proficiencies, features, spells, languages and feats of a character into what the
/// sheet lists. The review step and the saved sheet (and so the PDF) both fill their lists here,
/// so the two cannot list different things for the same character.
/// </summary>
public sealed class CharacterSheetContentLoader(
    IProficiencyRepository proficiencyRepository,
    IFeatureRepository featureRepository,
    ISpellRepository spellRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository)
{
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IFeatureRepository _featureRepository = featureRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IFeatRepository _featRepository = featRepository;

    public async Task<CharacterSheetContent> LoadAsync(
        List<string> proficiencyIds,
        List<string> featureIds,
        List<string> spellIds,
        IReadOnlyCollection<string> languageIds,
        List<string> featIds,
        IEnumerable<FeatSpellPick> featSpells,
        CancellationToken cancellationToken)
    {
        var proficiencies = await _proficiencyRepository.GetByIdsAsync(proficiencyIds, cancellationToken);
        var features = await _featureRepository.GetByIdsAsync(featureIds, cancellationToken);
        var spells = spellIds.Count > 0
            ? await _spellRepository.GetByIdsAsync(spellIds, cancellationToken)
            : [];
        var languages = await _languageRepository.GetByIdsAsync(languageIds, cancellationToken);
        var feats = await _featRepository.GetByIdsAsync(featIds, cancellationToken);

        return new CharacterSheetContent(
            proficiencies, features, spells, languages, CharacterFeatEntries.Build(featIds, featSpells, feats, spells));
    }
}
