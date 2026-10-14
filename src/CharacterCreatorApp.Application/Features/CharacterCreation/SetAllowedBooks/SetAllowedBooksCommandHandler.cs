using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAllowedBooks;

public class SetAllowedBooksCommandHandler(
    ICharacterDraftRepository characterDraftRepository,
    IBookRepository bookRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    IBackgroundRepository backgroundRepository,
    IFeatRepository featRepository,
    ISpellRepository spellRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<SetAllowedBooksCommand, Response>
{
    private readonly ICharacterDraftRepository _characterDraftRepository = characterDraftRepository;
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IFeatRepository _featRepository = featRepository;
    private readonly ISpellRepository _spellRepository = spellRepository;
    private readonly IUserContext _userContext = userContext;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Response> Handle(SetAllowedBooksCommand command, CancellationToken cancellationToken)
    {
        var draft = await _characterDraftRepository.GetByIdForUpdateAsync(command.DraftId, _userContext.UserId, cancellationToken);

        if (draft == null)
        {
            return CharacterDraftErrors.NotFoundById(command.DraftId);
        }

        var books = await _bookRepository.GetAllForEditionAsync(draft.Edition, cancellationToken);
        var unknown = command.Books.Except(books.Select(b => b.Code), StringComparer.Ordinal).ToList();

        if (unknown.Count != 0)
        {
            return CharacterDraftErrors.UnknownBooks(unknown, draft.Edition);
        }

        draft.AllowBooks(command.Books, await LoadChoiceSourcesAsync(draft, cancellationToken));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Response.Success();
    }

    // The book of every choice the draft holds, so the draft can drop the ones no longer allowed
    private async Task<DraftChoiceSources> LoadChoiceSourcesAsync(CharacterDraft draft, CancellationToken cancellationToken)
    {
        var race = draft.RaceId is { } raceId ? await _raceRepository.GetByIdWithSubracesAsync(raceId, cancellationToken) : null;
        var @class = draft.ClassId is { } classId ? await _classRepository.GetByIdWithSubclassesAsync(classId, cancellationToken) : null;
        var background = draft.BackgroundId is { } backgroundId ? await _backgroundRepository.GetByIdAsync(backgroundId, cancellationToken) : null;
        var feats = await _featRepository.GetByIdsAsync(draft.SelectedFeatIds.Concat(draft.FightingStyleIds).Distinct(), cancellationToken);
        var spells = await _spellRepository.GetByIdsAsync(draft.SelectedSpellIds, cancellationToken);

        return new DraftChoiceSources(
            race?.Source,
            race?.Subraces.FirstOrDefault(s => s.Id == draft.SubraceId)?.Source,
            @class?.Source,
            @class?.Subclasses.FirstOrDefault(s => s.Id == draft.SubclassId)?.Source,
            background?.Source,
            feats.ToDictionary(f => f.Id, f => f.Source),
            spells.ToDictionary(s => s.Id, s => s.Source));
    }
}
