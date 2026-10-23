using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

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
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IUserContext _userContext = userContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<Response<Guid>> Handle(StartCharacterEditCommand command, CancellationToken cancellationToken)
    {
        // Only the owner's characters are found, so someone else's is simply not there
        var character = await _characterRepository.GetByIdAsync(command.CharacterId, _userContext.UserId, cancellationToken);

        if (character == null)
        {
            return CharacterErrors.NotFoundById(command.CharacterId);
        }

        if (await _characterDraftRepository.GetEditAsync(character.Id, _userContext.UserId, cancellationToken) is { } unsaved)
        {
            await _characterDraftRepository.DeleteAsync(unsaved, cancellationToken);
        }

        var build = await _characterDraftRepository.GetBuildAsync(character.Id, _userContext.UserId, cancellationToken);
        var draft = build != null
            ? CharacterDraft.StartEdit(build, _timeProvider)
            : CharacterDraft.StartEdit(character, await BooksOfAsync(character, cancellationToken), _timeProvider);

        await _characterDraftRepository.CreateAsync(draft, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return draft.Id;
    }

    // The books the character's race, class and background come from, which the draft must allow
    private async Task<IEnumerable<string>> BooksOfAsync(Character character, CancellationToken cancellationToken)
    {
        var race = await _raceRepository.GetByIdWithSubracesAsync(character.RaceId, cancellationToken);
        var @class = await _classRepository.GetByIdWithSubclassesAsync(character.ClassId, cancellationToken);
        var background = await _backgroundRepository.GetByIdAsync(character.BackgroundId, cancellationToken);

        return new[]
            {
                race?.Source,
                race?.Subraces.FirstOrDefault(s => s.Id == character.SubraceId)?.Source,
                @class?.Source,
                @class?.Subclasses.FirstOrDefault(s => s.Id == character.SubclassId)?.Source,
                background?.Source,
            }
            .OfType<string>();
    }
}
