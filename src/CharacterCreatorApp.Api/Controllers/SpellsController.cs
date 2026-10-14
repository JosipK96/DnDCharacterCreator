using CharacterCreatorApp.Application.Features.Lookup.GetSpells;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/spells")]
    public class SpellsController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        [ProducesResponseType(typeof(List<SpellApiResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSpells(
            CancellationToken cancellationToken,
            [FromQuery] string? classId = null,
            [FromQuery] string? subclassId = null,
            [FromQuery] int? level = null,
            [FromQuery] int? maxLevel = null,
            [FromQuery] string? school = null,
            [FromQuery] bool? isRitual = null,
            [FromQuery] string[]? sources = null)
        {
            var query = new GetSpellsQuery(classId, subclassId, level, maxLevel, school, isRitual, sources);
            var response = await _sender.Send(query, cancellationToken);

            return OkOrProblem(response, spells => _mapper.Map<List<SpellApiResponse>>(spells));
        }
    }
}
