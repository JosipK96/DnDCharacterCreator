using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Feats;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetFeatEligibility;

/// <summary>
/// What the improvements step's feat list shows: which feats the character can take, and what one
/// it can't needs. It is worked out by FeatPrerequisiteCheck, as the step's save checks the picks,
/// so the client decides nothing. It is a what-if: the save still checks every pick.
/// </summary>
public class GetFeatEligibilityQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IFeatRepository featRepository,
    IUserContext userContext) : IQueryHandler<GetFeatEligibilityQuery, Response<List<FeatEligibilityReadModel>>>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<List<FeatEligibilityReadModel>>> Handle(GetFeatEligibilityQuery query, CancellationToken cancellationToken)
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

        // The class with its features: a Fighting Style feat asks for the feature of that name
        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        var race = string.IsNullOrEmpty(draft.RaceId) ? null : await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);
        var subrace = race?.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var feats = await _featRepository.GetAllAsync(draft.Edition, cancellationToken);

        // The points so far as one improvement: a minimum counts their total either way
        var increases = query.Increases
            .GroupBy(AbilityScore.Normalize)
            .ToDictionary(points => points.Key, points => points.Count());

        return FeatPrerequisiteCheck.Eligibility(
            feats.Where(f => draft.AllowsSource(f.Source)),
            [increases],
            draft,
            race,
            subrace,
            @class,
            @class.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId),
            level);
    }
}
