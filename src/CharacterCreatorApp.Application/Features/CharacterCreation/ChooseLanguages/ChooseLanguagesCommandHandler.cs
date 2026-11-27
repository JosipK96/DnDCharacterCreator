using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;

public class ChooseLanguagesCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IClassRepository classRepository,
    ILanguageRepository languageRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<ChooseLanguagesCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly DraftSelectionsLoader _selections = new(raceRepository, classRepository, backgroundRepository);
    private readonly ILanguageRepository _languageRepository = languageRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseLanguagesCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        var selections = await _selections.LoadRaceAndBackgroundAsync(draft, "selecting languages", cancellationToken);

        if (!selections.IsSuccess)
        {
            return selections.Errors;
        }

        var (race, background) = selections.Value;

        // A high elf picks one more than an elf
        var subrace = race.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var totalChoices = race.LanguageChoiceCountWith(subrace) + background.LanguageChoiceCount;

        if (totalChoices == 0)
        {
            draft.SetLanguages([]);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        if (command.LanguageIds.Count != totalChoices)
        {
            return CharacterDraftErrors.InvalidLanguageCount(totalChoices, command.LanguageIds.Count);
        }

        if (command.LanguageIds.Count != command.LanguageIds.Distinct().Count())
        {
            return CharacterDraftErrors.DuplicateLanguages();
        }

        // A 2024 character picks from the 2024 book's standard languages
        var choosable = await _languageRepository.GetChoosableAsync(draft.Edition, cancellationToken);
        var choosableIds = choosable.Select(l => l.Id).ToHashSet();

        var invalid = command.LanguageIds.Where(id => !choosableIds.Contains(id)).ToList();

        if (invalid.Count != 0)
        {
            return CharacterDraftErrors.InvalidLanguageIds(invalid);
        }

        // A pick the character already has from their race or background would be wasted: the
        // built character knows each language once, so it would end up a language short.
        var alreadyKnown = LanguageResolver
            .ResolveIds(race.RaceLanguages.Select(l => l.Language).Concat(background.NamedLanguages), choosable)
            .ToHashSet();

        var repeated = choosable
            .Where(l => alreadyKnown.Contains(l.Id) && command.LanguageIds.Contains(l.Id))
            .Select(l => l.Name)
            .ToList();

        if (repeated.Count != 0)
        {
            return CharacterDraftErrors.LanguagesAlreadyKnown(repeated);
        }

        draft.SetLanguages(command.LanguageIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}
