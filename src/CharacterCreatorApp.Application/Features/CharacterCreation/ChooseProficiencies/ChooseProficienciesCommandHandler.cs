using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Building;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;

public class ChooseProficienciesCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IProficiencyRepository proficiencyRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<ChooseProficienciesCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IProficiencyRepository _proficiencyRepository = proficiencyRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseProficienciesCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        if (string.IsNullOrEmpty(draft.ClassId))
        {
            return CharacterDraftErrors.ClassRequired("selecting proficiencies");
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(draft.ClassId, cancellationToken);

        if (@class == null)
        {
            return ClassErrors.NotFoundById(draft.ClassId);
        }

        if (@class.ProficiencyChoice == null)
        {
            draft.SetClassProficiencies([]);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        var requiredCount = @class.ProficiencyChoice.Count;

        if (command.ProficiencyIds.Count != requiredCount)
        {
            return CharacterDraftErrors.InvalidProficiencyCount(requiredCount, @class.ProficiencyChoice.Type, draft.ClassId, command.ProficiencyIds.Count);
        }

        var availableOptions = @class.ProficiencyChoice.Options
            .Select(o => o.ProficiencyId)
            .ToHashSet();

        var invalidProficiencies = command.ProficiencyIds
            .Where(id => !availableOptions.Contains(id))
            .ToList();

        if (invalidProficiencies.Any())
        {
            return CharacterDraftErrors.InvalidProficiencySelection(invalidProficiencies, availableOptions);
        }

        if (command.ProficiencyIds.Count != command.ProficiencyIds.Distinct().Count())
        {
            return CharacterDraftErrors.DuplicateProficiencies();
        }

        // A skill the race, its picks or the background give already is picked again in vain
        var race = string.IsNullOrEmpty(draft.RaceId)
            ? null
            : await _raceRepository.GetByIdWithDetailsAsync(draft.RaceId, cancellationToken);
        var background = string.IsNullOrEmpty(draft.BackgroundId)
            ? null
            : await _backgroundRepository.GetByIdWithDetailsAsync(draft.BackgroundId, cancellationToken);
        var repeated = ProficiencyPickCheck.FindRepeated(
            draft,
            race,
            race?.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId),
            background,
            @class,
            classPicks: command.ProficiencyIds);

        if (repeated is not null)
        {
            return repeated.Value;
        }

        foreach (var proficiencyId in command.ProficiencyIds)
        {
            var exists = await _proficiencyRepository.GetByIdAsync(proficiencyId, cancellationToken);

            if (exists == null)
            {
                return ProficiencyErrors.NotFoundById(proficiencyId);
            }
        }

        draft.SetClassProficiencies(command.ProficiencyIds);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}