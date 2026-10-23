using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

/// <summary>
/// Keeps the hit die's result for the level a level-up reaches. Only a level-up asks for one:
/// the levels a character is created with take the fixed value.
/// </summary>
public class SetHitPointsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<SetHitPointsCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(SetHitPointsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (!draft.IsLevelUp)
        {
            return CharacterDraftErrors.HitPointsOnlyOnLevelUp();
        }

        // A level-up keeps its class, so the draft always names one
        var @class = await _classRepository.GetByIdAsync(draft.ClassId!, cancellationToken);

        if (@class == null)
        {
            return CharacterErrors.ClassNotFoundById(draft.ClassId!);
        }

        if (command.Result > CharacterStatsCalculator.GetHitDieSize(@class.HitDie))
        {
            return CharacterDraftErrors.HitDieResultOutOfRange(command.Result, @class.HitDie);
        }

        draft.SetHitDieResult(command.Result);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}
