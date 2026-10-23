using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;

/// <summary>
/// Copies the character's build into a new draft, or, for a character no build is kept for, what
/// the character itself stores. An edit left unsaved earlier is dropped: an edit always starts
/// from the character as it is saved. Finalizing the draft replaces the character.
/// </summary>
public class StartCharacterEditCommandHandler(
    ICharacterRepository characterRepository,
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    TimeProvider timeProvider) : ICommandHandler<StartCharacterEditCommand, Response<Guid>>
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    private readonly CharacterEditStarter _editStarter = new(
        characterDraftRepository, raceRepository, classRepository, backgroundRepository, timeProvider);

    public async Task<Response<Guid>> Handle(StartCharacterEditCommand command, CancellationToken cancellationToken)
    {
        // Only the owner's characters are found, so someone else's is simply not there
        var character = await _characterRepository.GetByIdAsync(command.CharacterId, _userContext.UserId, cancellationToken);

        if (character == null)
        {
            return CharacterErrors.NotFoundById(command.CharacterId);
        }

        var draft = await _editStarter.StartAsync(character, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }
}
