using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetLanguages;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers;

[Route("api/languages")]
public class LanguagesController(ISender sender, IMapper mapper) : ApiController
{
    private readonly ISender _sender = sender;
    private readonly IMapper _mapper = mapper;

    /// <summary>The languages a character of one rules edition may pick; the 2014 ones without one.</summary>
    [HttpGet("get-list")]
    [ProducesResponseType(typeof(List<LanguageApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLanguages([FromQuery] string? edition, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetLanguagesQuery(edition), cancellationToken);

        return OkOrProblem(response, languages => _mapper.Map<List<LanguageApiResponse>>(languages));
    }
}
