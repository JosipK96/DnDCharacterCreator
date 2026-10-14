using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground
{
    public class SetCharacterBackgroundCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IBackgroundRepository backgroundRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext) : ICommandHandler<SetCharacterBackgroundCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUserContext _userContext = userContext;

        public async Task<Response> Handle(SetCharacterBackgroundCommand command, CancellationToken cancellationToken)
        {
            var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

            if (draft == null)
            {
                return CharacterDraftErrors.NotFoundById(command.DraftId);
            }

            var background = await _backgroundRepository.GetByIdWithDetailsAsync(command.BackgroundId, cancellationToken);

            if (background == null)
            {
                return BackgroundErrors.NotFoundById(command.BackgroundId);
            }

            if (background.Edition != draft.Edition)
            {
                return CharacterDraftErrors.EditionMismatch("background", background.Name, background.Edition, draft.Edition);
            }

            if (!draft.AllowsSource(background.Source))
            {
                return CharacterDraftErrors.SourceNotAllowed("background", background.Name, background.Source);
            }

            if (!background.AllowsAbilityIncreases(command.AbilityIncreases))
            {
                return CharacterDraftErrors.InvalidBackgroundAbilityIncreases(
                    background.Name, background.AbilityScoreOptions.ConvertAll(id => AbilityScore.FromId(id).Name));
            }

            draft.SetBackground(background.Id, command.AbilityIncreases);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }
    }
}
