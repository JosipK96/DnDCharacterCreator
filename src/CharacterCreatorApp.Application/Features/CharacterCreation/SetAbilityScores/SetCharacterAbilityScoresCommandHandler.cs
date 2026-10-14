using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores
{
    public class SetCharacterAbilityScoresCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext) : ICommandHandler<SetCharacterAbilityScoresCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserContext _userContext = userContext;

        public async Task<Response> Handle(SetCharacterAbilityScoresCommand command, CancellationToken cancellationToken)
        {
            var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (draft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            draft.SetBaseAbilityScores(command.Scores, command.Method);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Response.Success();
        }
    }
}
