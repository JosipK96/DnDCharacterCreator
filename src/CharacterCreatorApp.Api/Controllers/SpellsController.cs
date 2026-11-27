using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetSpells;
using CharacterCreatorApp.Contracts.Responses.Spells;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/spells")]
    public class SpellsController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        [ProducesResponseType(typeof(List<SpellApiResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSpells(
            CancellationToken cancellationToken,
            [FromQuery] string? classId = null,
            [FromQuery] string? subclassId = null,
            [FromQuery] int? maxLevel = null,
            [FromQuery] string[]? sources = null)
        {
            var query = new GetSpellsQuery(classId, subclassId, maxLevel, sources);
            var response = await _sender.Send(query, cancellationToken);

            return OkOrProblem(response, spells => spells.Select(spell => spell.ToResponse()).ToList());
        }
    }
}
