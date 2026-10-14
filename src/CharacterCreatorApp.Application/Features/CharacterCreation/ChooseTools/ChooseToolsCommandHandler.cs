using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseTools;

/// <summary>
/// Saves the tools picked for what the race, background and class offer (ToolPicks.SlotsFor),
/// one pick per choice. A tool the character already has, or one picked twice, is refused.
/// </summary>
public class ChooseToolsCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IClassRepository classRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<ChooseToolsCommand, Response>
{
    private const string Step = "picking tools";

    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseToolsCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
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

        var invalid = ToolPicks.FindInvalidPick(command.Picks, ToolPicks.SlotsFor(race, background, @class));

        if (invalid is not null)
        {
            return invalid.Value;
        }

        var subrace = race.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId);
        var repeated = ProficiencyPickCheck.FindRepeated(draft, race, subrace, background, @class, toolPicks: command.Picks);

        if (repeated is not null)
        {
            return repeated.Value;
        }

        draft.ChooseTools(command.Picks);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}
