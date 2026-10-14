using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetBackgrounds;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/backgrounds")]
    public class BackgroundsController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        /// <summary>
        /// The backgrounds of one rules edition ("2014" or "2024"), or of both without one, from
        /// the books named by sources ("XPHB", "EFA"), or from every book without any.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<BackgroundApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetBackgrounds([FromQuery] string? edition, [FromQuery] string[] sources, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetBackgroundsQuery(edition, sources), cancellationToken);

            return OkOrProblem(response, backgrounds => _mapper.Map<List<BackgroundApiResponse>>(backgrounds));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BackgroundApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBackgroundById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetBackgroundByIdQuery(id), cancellationToken);

            return OkOrProblem(response, background => _mapper.Map<BackgroundApiResponse>(background));
        }
    }
}
