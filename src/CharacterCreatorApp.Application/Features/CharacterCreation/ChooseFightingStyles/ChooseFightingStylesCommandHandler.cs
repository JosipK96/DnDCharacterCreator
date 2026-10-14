using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseFightingStyles;

public class ChooseFightingStylesCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IFeatRepository featRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<ChooseFightingStylesCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Response> Handle(ChooseFightingStylesCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired("choosing a Fighting Style");
        }

        if (draft.Level is not { } level)
        {
            return CharacterDraftErrors.LevelRequired("choosing a Fighting Style");
        }

        // With its features and subclasses: what is owed comes from the features named for it
        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        var subclass = @class.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId);
        var feats = await _featRepository.GetByIdsAsync(command.FeatIds.Concat(draft.SelectedFeatIds).Distinct(), cancellationToken);

        var invalidPick = FightingStyleChoice.FindInvalidPick(
            command.FeatIds, feats.ToDictionary(f => f.Id), draft.SelectedFeatIds, draft, @class, subclass, level);

        if (invalidPick is not null)
        {
            return invalidPick.Value;
        }

        draft.ChooseFightingStyles(command.FeatIds);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Response.Success();
    }
}
