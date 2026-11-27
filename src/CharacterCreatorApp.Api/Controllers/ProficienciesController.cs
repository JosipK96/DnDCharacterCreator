using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetProficiencies;
using CharacterCreatorApp.Contracts.Responses.Proficiencies;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/proficiencies")]
    public class ProficienciesController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        [ProducesResponseType(typeof(List<ProficiencyApiResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAllProficienciesQuery(), cancellationToken);

            return OkOrProblem(response, proficiencies => proficiencies.Select(proficiency => proficiency.ToResponse()).ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProficiencyApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetProficiencyByIdQuery(id), cancellationToken);

            return OkOrProblem(response, proficiency => proficiency.ToResponse());
        }
    }
}
