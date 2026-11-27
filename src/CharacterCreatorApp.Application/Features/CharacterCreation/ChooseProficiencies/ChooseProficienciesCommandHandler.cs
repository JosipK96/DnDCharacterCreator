using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;
using CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Loaders;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;

public class ChooseProficienciesCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IClassRepository classRepository,
    IRaceRepository raceRepository,
    IBackgroundRepository backgroundRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : ICommandHandler<ChooseProficienciesCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly DraftSelectionsLoader _selections = new(raceRepository, classRepository, backgroundRepository);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    public async Task<Response> Handle(ChooseProficienciesCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        var classResult = await _selections.LoadClassAsync(draft, "selecting proficiencies", cancellationToken);

        if (!classResult.IsSuccess)
        {
            return classResult.Errors;
        }

        var @class = classResult.Value;

        if (@class.ProficiencyChoice == null)
        {
            draft.SetClassProficiencies([]);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Response.Success();
        }

        var requiredCount = @class.ProficiencyChoice.Count;

        if (command.ProficiencyIds.Count != requiredCount)
        {
            return CharacterDraftErrors.InvalidProficiencyCount(requiredCount, @class.ProficiencyChoice.Type, @class.Id, command.ProficiencyIds.Count);
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
        var race = await _selections.LoadRaceIfChosenAsync(draft, cancellationToken);
        var background = await _selections.LoadBackgroundIfChosenAsync(draft, cancellationToken);
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

        // No lookup per id: every id passed the class's options above, and an option's
        // ProficiencyId is a foreign key to Proficiencies, so it is a real proficiency
        draft.SetClassProficiencies(command.ProficiencyIds);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }
}