using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Errors;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Application.ReadModels.Races;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Feats;

namespace CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

/// <summary>
/// Counts what a wizard list leaves out for the books the draft has not ticked. Every entry is
/// counted under the book that holds it, and only the edition's books are counted, so an entry
/// of the other edition's books never is. The Player's Handbook is always ticked. The spells
/// step's count is not here: GET spell-allowance builds it with the step's list, for the draft.
/// </summary>
public class GetHiddenOptionsQueryHandler(
    IBookRepository bookRepository,
    IRaceRepository raceRepository,
    IClassRepository classRepository,
    ISubclassRepository subclassRepository,
    IBackgroundRepository backgroundRepository,
    IFeatRepository featRepository)
    : IQueryHandler<GetHiddenOptionsQuery, Response<HiddenOptionsReadModel>>
{
    private readonly IBookRepository _bookRepository = bookRepository;
    private readonly IRaceRepository _raceRepository = raceRepository;
    private readonly IClassRepository _classRepository = classRepository;
    private readonly ISubclassRepository _subclassRepository = subclassRepository;
    private readonly IBackgroundRepository _backgroundRepository = backgroundRepository;
    private readonly IFeatRepository _featRepository = featRepository;

    public async Task<Response<HiddenOptionsReadModel>> Handle(GetHiddenOptionsQuery query, CancellationToken cancellationToken)
    {
        var books = await _bookRepository.GetAllForEditionAsync(query.Edition, cancellationToken);
        var ticked = books.Where(b => b.IsPlayersHandbook || query.Sources.Contains(b.Code)).Select(b => b.Code).ToHashSet();

        var sources = await SourcesOfAsync(query, ticked, cancellationToken);

        if (!sources.IsSuccess)
        {
            return sources.Errors;
        }

        return HiddenOptionsReadModel.Of(books, ticked.Contains, sources.Value);
    }

    // The book of each entry the list could offer, one per entry
    private async Task<Response<List<string>>> SourcesOfAsync(GetHiddenOptionsQuery query, HashSet<string> ticked, CancellationToken cancellationToken)
    {
        switch (query.List)
        {
            case HiddenOptionLists.Races:
                return RaceSources(await _raceRepository.GetAllAsync(query.Edition, cancellationToken), ticked);

            case HiddenOptionLists.Classes:
                return (await _classRepository.GetClassesAsync(query.Edition, cancellationToken)).ConvertAll(c => c.Source);

            case HiddenOptionLists.Backgrounds:
                return (await _backgroundRepository.GetAllAsync(query.Edition, cancellationToken)).ConvertAll(b => b.Source);

            case HiddenOptionLists.Feats:
                // The improvements step's list (a 2014 Fighting Style is no feat an improvement
                // buys), or a pick of one category's feats
                return (await _featRepository.GetAllAsync(query.Edition, cancellationToken))
                    .Where(f => FeatCategory.CanBeTakenForImprovement(f.Edition, f.Category))
                    .Where(f => query.Category is null || f.Category == query.Category)
                    .Select(f => f.Source)
                    .ToList();

            default:
                if (await _classRepository.GetByIdAsync(query.ClassId!, cancellationToken) is null)
                {
                    return ClassErrors.NotFoundById(query.ClassId!);
                }

                return (await _subclassRepository.GetByClassIdAsync(query.ClassId!, cancellationToken)).ConvertAll(s => s.Source);
        }
    }

    // A subrace counts when its race is ticked or in its own book: one whose race needs another
    // unticked book still would not be offered after ticking the subrace's
    private static List<string> RaceSources(List<RaceReadModel> races, HashSet<string> ticked) =>
        races
            .Select(r => r.Source)
            .Concat(races.SelectMany(r => r.Subraces
                .Where(s => ticked.Contains(r.Source) || s.Source == r.Source)
                .Select(s => s.Source)))
            .ToList();
}
