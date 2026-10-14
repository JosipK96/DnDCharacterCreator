using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetProficiencies;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/proficiencies")]
    public class ProficienciesController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        [ProducesResponseType(typeof(List<ProficiencyApiResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetAllProficienciesQuery(), cancellationToken);

            return OkOrProblem(response, proficiencies => _mapper.Map<List<ProficiencyApiResponse>>(proficiencies));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ProficiencyApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetProficiencyByIdQuery(id), cancellationToken);

            return OkOrProblem(response, proficiency => _mapper.Map<ProficiencyApiResponse>(proficiency));
        }
    }
}
