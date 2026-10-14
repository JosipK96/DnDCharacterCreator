using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetFeats;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/feats")]
    public class FeatsController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        /// <summary>The feats of one rules edition ("2014" or "2024"), or of both without one.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<FeatApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFeats([FromQuery] string? edition, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetFeatsQuery(edition), cancellationToken);

            return OkOrProblem(response, feats => _mapper.Map<List<FeatApiResponse>>(feats));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FeatApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFeatById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetFeatByIdQuery(id), cancellationToken);

            return OkOrProblem(response, feat => _mapper.Map<FeatApiResponse>(feat));
        }

        [HttpGet("by-proficiency/{proficiencyId}")]
        [ProducesResponseType(typeof(List<FeatApiResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatsByProficiency(string proficiencyId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetFeatsByProficiencyIdQuery(proficiencyId), cancellationToken);

            return OkOrProblem(response, feats => _mapper.Map<List<FeatApiResponse>>(feats));
        }
    }
}
