using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetBooks;
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
}
