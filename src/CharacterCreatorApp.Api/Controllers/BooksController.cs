using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetBooks;
using CharacterCreatorApp.Application.Features.Lookup.GetHiddenOptions;
using CharacterCreatorApp.Contracts.Responses.Books;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers;

[Route("api/books")]
public class BooksController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    /// <summary>The books of one rules edition ("2014" or "2024"), its Player's Handbook first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BookApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBooks([FromQuery] string edition, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetBooksQuery(edition), cancellationToken);

        return OkOrProblem(response, books => books.Select(book => book.ToResponse()).ToList());
    }

    /// <summary>
    /// How many entries of a wizard list ("races", "classes", "subclasses", "backgrounds" or
    /// "feats") the edition's books that sources leaves out hold, and in which books. Subclasses
    /// are one class's (classId); feats narrow to one category, for a pick of only those. The
    /// spells step's count comes with its list, from GET character-drafts/{id}/spell-allowance.
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
        [FromQuery] string? category = null)
    {
        var query = new GetHiddenOptionsQuery(edition, list, sources, classId, category);
        var response = await _sender.Send(query, cancellationToken);

        return OkOrProblem(response, hidden => hidden.ToResponse());
    }
}
