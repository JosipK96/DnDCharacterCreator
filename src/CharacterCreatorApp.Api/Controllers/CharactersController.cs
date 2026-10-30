using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Api.Mapping;
using CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;
using CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;
using CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterDetails;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetCharacterSheetPdf;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Application.Features.CharacterManagement.LevelUpCharacter;
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

        /// <summary>
        /// The character sheet as a PDF to print: the familiar first page, then features and spells
        /// on as many pages as they take. ?paper=letter lays it out for US Letter, else A4.
        /// </summary>
        [HttpGet("{id:guid}/sheet.pdf")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCharacterSheetPdf(Guid id, [FromQuery] SheetPaper paper, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCharacterSheetPdfQuery(id, paper), cancellationToken);

            return FileOrProblem(response, sheet => File(sheet.Content, "application/pdf", sheet.FileName));
        }

        /// <summary>
        /// Opens the character as a draft with every choice it was made with, for the wizard; the
        /// draft's id comes back. Finalizing that draft (POST api/characters) replaces the character
        /// and keeps its id; leaving it unsaved changes nothing. Editing again starts over.
        /// </summary>
        [HttpPost("{id:guid}/edit")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EditCharacter(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new StartCharacterEditCommand(id), cancellationToken);

            return OkOrProblem(response);
        }

        /// <summary>
        /// Opens the character one level up as a draft, with every earlier pick kept; the draft's id
        /// comes back. Its LevelUpChoices are the steps the new level asks for: its hit points, and
        /// a subclass, an improvement or spells where the class data gives one. Finalizing it
        /// (POST api/characters) replaces the character and keeps its id.
        /// </summary>
        [HttpPost("{id:guid}/level-up")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LevelUpCharacter(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new StartCharacterLevelUpCommand(id), cancellationToken);

            return OkOrProblem(response);
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
