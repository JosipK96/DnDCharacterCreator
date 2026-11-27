using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetLanguages;
using CharacterCreatorApp.Contracts.Responses.Languages;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers;

[Route("api/languages")]
public class LanguagesController(ISender sender) : ApiController
{
    private readonly ISender _sender = sender;

    /// <summary>The languages a character of one rules edition may pick; the 2014 ones without one.</summary>
    [HttpGet("get-list")]
    [ProducesResponseType(typeof(List<LanguageApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLanguages([FromQuery] string? edition, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetLanguagesQuery(edition), cancellationToken);

        return OkOrProblem(response, languages => languages.Select(language => language.ToResponse()).ToList());
    }
}
