using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.CreateDraft
{
    public class CreateCharacterDraftCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : ICommandHandler<CreateCharacterDraftCommand, Response<Guid>>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IUserContext _userContext = userContext;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task<Response<Guid>> Handle(CreateCharacterDraftCommand command, CancellationToken cancellationToken)
        {
            // A player keeps one unfinished new character: a second tab or a retried request gets
            // the one already started back, and "Start Fresh" deletes it before creating (#90).
            // shortcut: two creates at the same instant can still both insert; a partial unique
            // index (with a migration clearing existing duplicates) closes that if it ever shows up
            var existing = await _characterDraftRepository.GetIncompleteDraft(_userContext.UserId, cancellationToken);

            if (existing is not null)
            {
                return existing.Id;
            }

            var draft = CharacterDraft.CreateEmpty(_userContext.UserId, _timeProvider);

            await _characterDraftRepository.CreateAsync(draft, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return draft.Id;
        }
    }
}
