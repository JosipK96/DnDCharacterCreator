using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

/// <summary>
/// Keeps the hit die's results for the levels the draft rolls for (CharacterDraft.HitDieLevels):
/// a level-up's new level, or every level from 2 of a new character or an edit. Level 1 is the
/// die's maximum, so it has no result to keep.
/// </summary>
public class SetHitPointsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<SetHitPointsCommand, Response>
{
    private const string Step = "setting hit points";

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

        if (draft.Level is not { } level)
        {
            return CharacterDraftErrors.LevelRequired(Step);
        }

        if (draft.ClassId is not { } classId)
        {
            return CharacterDraftErrors.ClassRequired(Step);
        }

        var levels = draft.HitDieLevels;
        var notRolled = command.Results.Keys.Where(l => !levels.Contains(l)).Order().ToList();

        if (notRolled.Count != 0)
        {
            return CharacterDraftErrors.HitDieLevelNotRolled(notRolled[0], levels);
        }

        if (draft.IsLevelUp && !command.Results.ContainsKey(level))
        {
            return CharacterDraftErrors.HitDieResultRequired(level);
        }

        var @class = await _classRepository.GetByIdAsync(classId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(classId);
        }

        var dieSize = HitDieNotation.SizeOf(@class.HitDie);
        var tooHigh = command.Results.Values.Where(result => result > dieSize).ToList();

        if (tooHigh.Count != 0)
        {
            return CharacterDraftErrors.HitDieResultOutOfRange(tooHigh[0], @class.HitDie);
        }

        draft.SetHitDieResults(command.Results);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}
