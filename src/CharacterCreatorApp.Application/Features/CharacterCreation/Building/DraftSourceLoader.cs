using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>
/// Loads the race, class, background, feats, languages and spells a draft names by id, so a
/// draft can be turned into a character. Shared by finalizing and previewing, which need the same set.
/// </summary>
public sealed class DraftSourceLoader(
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    ILanguageRepository languageRepository,
    IFeatRepository featRepository,
    ISpellRepository spellRepository)
{
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;

    public async Task<Response<CharacterSources>> LoadAsync(CharacterDraft draft, CancellationToken cancellationToken)
    {
        var race = await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId!, cancellationToken);

        if (race == null)
        {
            return CharacterErrors.RaceNotFoundById(draft.RaceId!);
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId!, cancellationToken);

        if (@class == null)
        {
            return CharacterErrors.ClassNotFoundById(draft.ClassId!);
        }

        var background = await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId!, cancellationToken);

        if (background == null)
        {
            return CharacterErrors.BackgroundNotFoundById(draft.BackgroundId!);
        }

        var subclass = string.IsNullOrWhiteSpace(draft.SubclassId)
            ? null
            : @class.Subclasses.FirstOrDefault(sc => sc.Id == draft.SubclassId);

        if (!string.IsNullOrWhiteSpace(draft.SubclassId) && subclass == null)
        {
            return CharacterErrors.SubclassNotFoundById(draft.SubclassId!);
        }

        var subrace = string.IsNullOrWhiteSpace(draft.SubraceId)
            ? null
            : race.Subraces.FirstOrDefault(sr => sr.Id == draft.SubraceId);

        if (!string.IsNullOrWhiteSpace(draft.SubraceId) && subrace == null)
        {
            return CharacterErrors.SubraceNotFoundById(draft.SubraceId!);
        }

        // The background's feat grants what the others do: a 2024 Tavern Brawler's improvised weapons
        var feats = await _featRepository.GetByIdsAsync(
            CharacterBuilder.BackgroundFeatIds(background).Concat(draft.SelectedFeatIds).Distinct(), cancellationToken);
        var languages = await _languageRepository.GetAllForEditionAsync(draft.Edition, cancellationToken);
        var choosableSpells = @class.SpellAllowanceAt(draft.Level!.Value, subclass) == null
            ? []
            : await SpellChoice.LoadChoosableAsync(_spellRepository, @class, subclass, cancellationToken);

        return new CharacterSources(race, subrace, @class, subclass, background, feats, languages, choosableSpells);
    }
}
