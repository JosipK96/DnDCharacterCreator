using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels.Books;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;

/// <summary>
/// How many entries of one wizard list (<see cref="HiddenOptionLists"/>) the edition's books that
/// Sources leaves out hold, and in which books: what the step would offer once those are ticked.
/// ClassId names the class whose subclasses are counted. Category narrows the feats to one
/// category, for a pick of only those (a 2024 human's Origin feat, a Dark Gift).
/// </summary>
public record GetHiddenOptionsQuery(
    string Edition,
    string List,
    IReadOnlyCollection<string> Sources,
    string? ClassId = null,
    string? Category = null
) : IQuery<Response<HiddenOptionsReadModel>>;
