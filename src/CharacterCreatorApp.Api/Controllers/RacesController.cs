using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetRaces;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/races")]
    public class RacesController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        /// <summary>The races of one rules edition ("2014" or "2024"), or of both without one.</summary>
        [HttpGet("get-list")]
        [ProducesResponseType(typeof(List<RaceApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRaces([FromQuery] string? edition, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetRacesQuery(edition), cancellationToken);

            return OkOrProblem(response, races => _mapper.Map<List<RaceApiResponse>>(races));
        }
    }
}
