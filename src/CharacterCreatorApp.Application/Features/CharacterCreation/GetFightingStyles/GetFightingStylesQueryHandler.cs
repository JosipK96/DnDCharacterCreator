using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.Services;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetFightingStyles;

/// <summary>
/// What the wizard's Fighting Style section shows. The count and the options are worked out here,
/// from the class's features and the feats' prerequisites, so the client decides nothing.
/// </summary>
public class GetFightingStylesQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IFeatRepository featRepository,
    IUserContext userContext) : IQueryHandler<GetFightingStylesQuery, Response<FightingStylesReadModel>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<FightingStylesReadModel>> Handle(GetFightingStylesQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        if (string.IsNullOrEmpty(draft.ClassId) || draft.Level is not { } level)
        {
            return CharacterDraftErrors.ClassAndLevelRequired();
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        var subclass = @class.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId);
        var owed = CharacterFeatures.FightingStylesOwed(@class, subclass, level);

        if (owed == 0)
        {
            return new FightingStylesReadModel();
        }

        var feats = await _featRepository.GetAllAsync(draft.Edition, cancellationToken);

        return new FightingStylesReadModel
        {
            Owed = owed,
            Options = FightingStyleChoice.Choosable(feats, draft, @class, subclass, level)
        };
    }
}
