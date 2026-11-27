using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.Lookup.GetClasses;
using CharacterCreatorApp.Contracts.Responses.Classes;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Route("api/classes")]
    public class ClassesController(ISender sender) : ApiController
    {
        private readonly ISender _sender = sender;

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClassApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClassById(string id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetClassByIdQuery(id), cancellationToken);

            return OkOrProblem(response, @class => @class.ToResponse());
        }

        /// <summary>
        /// The classes of one rules edition ("2014" or "2024"), or of both without one, from
        /// the books named by sources ("XPHB", "EFA"), or from every book without any.
        /// </summary>
        [HttpGet("get-list")]
        [ProducesResponseType(typeof(List<ClassApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetClasses([FromQuery] string? edition, [FromQuery] string[] sources, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetClassesQuery(edition, sources), cancellationToken);

            return OkOrProblem(response, classes => classes.Select(@class => @class.ToResponse()).ToList());
        }
    }
}
