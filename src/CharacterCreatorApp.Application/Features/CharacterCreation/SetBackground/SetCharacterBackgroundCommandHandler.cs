using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground
{
    public class SetCharacterBackgroundCommandHandler(
        ICharacterDraftRepository characterDraftRepository,
        IBackgroundRepository backgroundRepository,
        IFeatRepository featRepository,
        IUnitOfWork unitOfWork,
        IUserContext userContext) : ICommandHandler<SetCharacterBackgroundCommand, Response>
    {
        private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
        private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
        private readonly IFeatRepository _featRepository = featRepository;
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

            if (command.FeatId is { } featId)
            {
                var featError = await FindInvalidFeatAsync(background, featId, draft, cancellationToken);

                if (featError is not null)
                {
                    return featError.Value;
                }
            }

            draft.SetBackground(background.Id, command.AbilityIncreases, command.FeatId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        // Only a background without a feat of its own takes one, and only from its category
        private async Task<Error?> FindInvalidFeatAsync(Background background, string featId, CharacterDraft draft, CancellationToken cancellationToken)
        {
            if (background.FeatChoiceCategory is not { } category)
            {
                return CharacterDraftErrors.BackgroundFeatNotChoosable(background.Name);
            }

            var feats = await _featRepository.GetByIdsAsync([featId], cancellationToken);

            if (feats.FirstOrDefault() is not { } feat)
            {
                return FeatErrors.NotFoundById(featId);
            }

            if (feat.Edition != draft.Edition)
            {
                return CharacterDraftErrors.EditionMismatch("feat", feat.Name, feat.Edition, draft.Edition);
            }

            if (!draft.AllowsSource(feat.Source))
            {
                return CharacterDraftErrors.SourceNotAllowed("feat", feat.Name, feat.Source);
            }

            return feat.Category == category ? null : CharacterDraftErrors.BackgroundFeatNotOfCategory(background.Name, feat.Name, category);
        }
    }
}
