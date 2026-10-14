using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;
using CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Contracts.Requests.Character;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Authorize]
    [Route("api/characters")]
    public class CharactersController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new FinalizeCharacterCommand(request.DraftId), cancellationToken);

            return OkOrProblem(response);
        }

        [HttpPost("get-list")]
        [ProducesResponseType(typeof(FilterResponse<CharacterSummaryApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyCharacters([FromBody] FilterRequest<GetCharactersApiRequest>? filterRequest, CancellationToken cancellationToken)
        {
            var filter = filterRequest.ToFilterOptions<GetCharactersApiRequest, FilterCharacterSearchParams>();

            var response = await _sender.Send(new GetMyCharactersQuery(filter), cancellationToken);

            return OkOrProblem(response, page => _mapper.Map<FilterResponse<CharacterSummaryApiResponse>>(page));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CharacterDetailsApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCharacter(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCharacterDetailsQuery(id), cancellationToken);

            return OkOrProblem(response, character => _mapper.Map<CharacterDetailsApiResponse>(character));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCharacter(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteCharacterCommand(id), cancellationToken);

            return NoContentOrProblem(response);
        }
    }
}
