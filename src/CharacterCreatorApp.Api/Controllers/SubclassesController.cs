using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetSubclasses;
using CharacterCreatorApp.Contracts.Responses.Classes;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/subclasses")]
    public class SubclassesController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpGet]
        [ProducesResponseType(typeof(List<SubclassApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubclassesByClass([FromQuery] string classId, [FromQuery] string[] sources, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubclassesQuery(classId, sources), cancellationToken);

            return OkOrProblem(response, subclasses => subclasses.Select(subclass => subclass.ToResponse()).ToList());
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SubclassApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubclassById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetSubclassByIdQuery(id), cancellationToken);

            return OkOrProblem(response, subclass => subclass.ToResponse());
        }
    }
}
