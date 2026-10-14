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
            var draft = CharacterDraft.CreateEmpty(_userContext.UserId, _timeProvider);

            await _characterDraftRepository.CreateAsync(draft, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return draft.Id;
        }
    }
}
