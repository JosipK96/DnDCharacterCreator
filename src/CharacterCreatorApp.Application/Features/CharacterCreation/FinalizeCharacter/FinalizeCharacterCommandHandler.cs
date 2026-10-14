using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter
{
    public class FinalizeCharacterCommandHandler(ICharacterDraftRepository characterDraftRepository,
        ICharacterRepository characterRepository,
        IRaceRepository raceRepository,
        IClassRepository classRepository,
        IBackgroundRepository backgroundRepository,
        ILanguageRepository languageRepository,
        IFeatRepository featRepository,
        ISpellRepository spellRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        TimeProvider timeProvider) : ICommandHandler<FinalizeCharacterCommand, Response<Guid>>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly ICharacterRepository _characterRepository = characterRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserContext _userContext = userContext;
        private readonly TimeProvider _timeProvider = timeProvider;

        private readonly DraftSourceLoader _sourceLoader = new(
            raceRepository, classRepository, backgroundRepository, languageRepository, featRepository, spellRepository);

        public async Task<Response<Guid>> Handle(FinalizeCharacterCommand command, CancellationToken cancellationToken)
        {
            var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (draft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            if (!draft.IsReadyToFinalize())
            {
                return CharacterDraftErrors.Invalid();
            }

            var sourcesResponse = await _sourceLoader.LoadAsync(draft, cancellationToken);

            if (!sourcesResponse.IsSuccess)
            {
                return sourcesResponse.Errors;
            }

            var sources = sourcesResponse.Value;

            var incompleteChoice = DraftCompletenessChecker.FindMissingChoice(draft, sources);

            if (incompleteChoice is not null)
            {
                return incompleteChoice.Value;
            }

            var character = CharacterBuilder.Build(_userContext.UserId, draft, sources, _timeProvider);

            await _characterRepository.Create(character, cancellationToken);
            draft.MarkAsCompleted();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return character.Id;
        }
    }
}
