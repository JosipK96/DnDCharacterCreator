using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/subclasses")]
    public class SubclassesController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        [HttpGet]
        [ProducesResponseType(typeof(List<SubclassApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubclassesByClass([FromQuery] string classId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubclassesQuery(classId), cancellationToken);

            return OkOrProblem(response, subclasses => _mapper.Map<List<SubclassApiResponse>>(subclasses));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SubclassApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubclassById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubclassByIdQuery(id), cancellationToken);

            return OkOrProblem(response, subclass => _mapper.Map<SubclassApiResponse>(subclass));
        }
    }
}
