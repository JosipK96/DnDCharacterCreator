using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.LevelUpCharacter;

/// <summary>
/// Opens the character as an edit does, then one level up (CharacterDraft.LevelUp): every pick
/// is kept, and the draft lists the steps the new level brings, from the class and subclass data.
/// An edit or level-up left unsaved earlier is dropped. Finalizing the draft replaces the character.
/// </summary>
public class StartCharacterLevelUpCommandHandler(
    ICharacterRepository characterRepository,
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    TimeProvider timeProvider) : ICommandHandler<StartCharacterLevelUpCommand, Response<Guid>>
{
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;

    private readonly CharacterEditStarter _editStarter = new(
        characterDraftRepository, raceRepository, classRepository, backgroundRepository, timeProvider);

    public async Task<Response<Guid>> Handle(StartCharacterLevelUpCommand command, CancellationToken cancellationToken)
    {
        // Only the owner's characters are found, so someone else's is simply not there
        var character = await _characterRepository.GetByIdAsync(command.CharacterId, _userContext.UserId, cancellationToken);

        if (character == null)
        {
            return CharacterErrors.NotFoundById(command.CharacterId);
        }

        if (character.Level >= CharacterDraft.MaxLevel)
        {
            return CharacterErrors.AtMaxLevel(character.Name, CharacterDraft.MaxLevel);
        }

        var @class = await _classRepository.GetByIdWithDetailsAsync(character.ClassId, cancellationToken);

        if (@class == null)
        {
            return CharacterErrors.ClassNotFoundById(character.ClassId);
        }

        var draft = await _editStarter.StartAsync(character, cancellationToken);

        // The final scores, which a 2014 cleric's prepared count follows
        draft.LevelUp(@class, @class.Subclasses.Find(s => s.Id == draft.SubclassId), character.AbilityScores);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }
}
