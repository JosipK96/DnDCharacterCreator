using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetBooks;
using CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers;

[Route("api/books")]
public class BooksController(ISender sender, IMapper mapper) : ApiController
{
    private readonly ISender _sender = sender;
    private readonly IMapper _mapper = mapper;

    /// <summary>The books of one rules edition ("2014" or "2024"), its Player's Handbook first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BookApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBooks([FromQuery] string edition, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetBooksQuery(edition), cancellationToken);

        return OkOrProblem(response, books => _mapper.Map<List<BookApiResponse>>(books));
    }

    /// <summary>
    /// How many entries of a wizard list ("races", "classes", "subclasses", "backgrounds", "feats"
    /// or "spells") the edition's books that sources leaves out hold, and in which books. Subclasses
    /// and spells are one class's (classId); spells narrow to what the spells step lists for a
    /// subclass and a highest spell level; feats narrow to one category, for a pick of only those.
    /// </summary>
    [HttpGet("hidden-options")]
    [ProducesResponseType(typeof(HiddenOptionsApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHiddenOptions(
        [FromQuery] string edition,
        [FromQuery] string list,
        [FromQuery] string[] sources,
        CancellationToken cancellationToken,
        [FromQuery] string? classId = null,
        [FromQuery] string? subclassId = null,
        [FromQuery] int? maxLevel = null,
        [FromQuery] string? category = null)
    {
        var query = new GetHiddenOptionsQuery(edition, list, sources, classId, subclassId, maxLevel, category);
        var response = await _sender.Send(query, cancellationToken);

        return OkOrProblem(response, hidden => _mapper.Map<HiddenOptionsApiResponse>(hidden));
    }
}
