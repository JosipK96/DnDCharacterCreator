using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Characters;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetDraftChoicesOwed;

/// <summary>
/// The counts the improvements, languages and hit points steps show, read from the race, class
/// and background as their step handlers and finalize check them, so the wizard keeps no copy of
/// the rules. A choice not made yet counts nothing rather than refusing, as the step it belongs
/// to will ask for it.
/// </summary>
public class GetDraftChoicesOwedQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    ILanguageRepository languageRepository,
    IUserContext userContext) : IQueryHandler<GetDraftChoicesOwedQuery, Response<DraftChoicesOwedReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly DraftSelectionsLoader _selections = new(raceRepository, classRepository, backgroundRepository);
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<DraftChoicesOwedReadModel>> Handle(GetDraftChoicesOwedQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        var race = await _selections.LoadRaceIfChosenAsync(draft, cancellationToken);
        var @class = await _selections.LoadClassIfChosenAsync(draft, cancellationToken);
        var background = await _selections.LoadBackgroundIfChosenAsync(draft, cancellationToken);

        // A variant human's feat and a high elf's language come with the subrace
        var subrace = race?.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var bonusFeats = race?.BonusFeatsWith(subrace);

        List<string> knownLanguageIds = [];

        if (race != null || background != null)
        {
            // A 2024 character picks from the 2024 book's standard languages
            var choosable = await _languageRepository.GetChoosableAsync(draft.Edition, cancellationToken);
            var given = (race?.RaceLanguages.Select(l => l.Language) ?? []).Concat(background?.NamedLanguages ?? []);
            knownLanguageIds = [.. LanguageResolver.ResolveIds(given, choosable).Distinct()];
        }

        return new DraftChoicesOwedReadModel
        {
            Improvements = @class != null && draft.Level is { } level ? @class.ImprovementsAt(level) : 0,
            ImprovementLevels = @class?.AsiLevels ?? [],
            BonusFeats = bonusFeats?.Count ?? 0,
            BonusFeatCategory = bonusFeats?.Category,
            BackgroundFeatId = background?.FeatIdWith(draft.BackgroundFeatId),
            Languages = (race?.LanguageChoiceCountWith(subrace) ?? 0) + (background?.LanguageChoiceCount ?? 0),
            KnownLanguageIds = knownLanguageIds,
            HitDieSize = @class?.HitDieSize ?? 0,
            HitDieFixedValue = @class?.HitDieFixedValue ?? 0,
        };
    }
}
