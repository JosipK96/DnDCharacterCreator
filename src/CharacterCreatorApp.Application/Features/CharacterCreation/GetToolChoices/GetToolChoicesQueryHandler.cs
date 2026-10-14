using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetToolChoices;

/// <summary>
/// What the proficiencies step asks below the class's skills: the tool choices the draft owes,
/// worked out as ChooseTools and finalize check them, so the client decides nothing.
/// </summary>
public class GetToolChoicesQueryHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IClassRepository classRepository,
    IProficiencyRepository proficiencyRepository,
    IUserContext userContext) : IQueryHandler<GetToolChoicesQuery, Response<List<ToolChoiceReadModel>>>
{
    private const string Step = "picking tools";

    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response<List<ToolChoiceReadModel>>> Handle(GetToolChoicesQuery query, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdAsync(query.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(query.DraftId);
        }

        if (string.IsNullOrEmpty(draft.RaceId))
        {
            return CharacterDraftErrors.RaceRequired(Step);
        }

        if (string.IsNullOrEmpty(draft.BackgroundId))
        {
            return CharacterDraftErrors.BackgroundRequired(Step);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired(Step);
        }

        var race = await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);

        if (race == null)
        {
            return RaceErrors.NotFoundById(draft.RaceId);
        }

        var background = await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId, cancellationToken);

        if (background == null)
        {
            return BackgroundErrors.NotFoundById(draft.BackgroundId);
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        var slots = ToolPicks.SlotsFor(race, background, @class);

        if (slots.Count == 0)
        {
            return new List<ToolChoiceReadModel>();
        }

        var proficiencies = (await _proficiencyRepository.GetByIdsAsync(
            slots.SelectMany(s => s.Choice.Options).Distinct().ToList(), cancellationToken)).ToDictionary(p => p.Id);

        return slots.ConvertAll(slot => new ToolChoiceReadModel
        {
            OfferedBy = slot.Name,
            Count = slot.Choice.Count,
            Options = slot.Choice.Options
                .Where(proficiencies.ContainsKey)
                .Select(id => proficiencies[id])
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList(),
        });
    }
}
