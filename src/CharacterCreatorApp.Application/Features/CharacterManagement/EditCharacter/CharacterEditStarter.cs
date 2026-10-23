using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;

/// <summary>
/// How an edit and a level-up both open a saved character: an edit or level-up left unsaved
/// earlier is dropped, so a character has one at most and it always starts from the character
/// as it is saved, and the new draft is a copy of the build, or, for a character no build is
/// kept for, of what the character itself stores. The caller saves.
/// </summary>
public class CharacterEditStarter(
    ICharacterDraftRepository characterDraftRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    TimeProvider timeProvider)
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<CharacterDraft> StartAsync(Character character, CancellationToken cancellationToken)
    {
        if (await _characterDraftRepository.GetEditAsync(character.Id, character.UserId, cancellationToken) is { } unsaved)
        {
            await _characterDraftRepository.DeleteAsync(unsaved, cancellationToken);
        }

        var build = await _characterDraftRepository.GetBuildAsync(character.Id, character.UserId, cancellationToken);
        var draft = build != null
            ? CharacterDraft.StartEdit(build, _timeProvider)
            : CharacterDraft.StartEdit(character, await BooksOfAsync(character, cancellationToken), _timeProvider);

        await _characterDraftRepository.CreateAsync(draft, cancellationToken);

        return draft;
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
