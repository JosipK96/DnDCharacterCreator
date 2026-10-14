using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel
{
    public class SetCharacterNameAndLevelCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext) : ICommandHandler<SetCharacterNameAndLevelCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserContext _userContext = userContext;

        public async Task<Response> Handle(SetCharacterNameAndLevelCommand command, CancellationToken cancellationToken)
        {
            var characterDraft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (characterDraft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            characterDraft.ChooseEdition(command.Edition);
            characterDraft.SetNameAndLevel(command.Name, command.Level);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}
