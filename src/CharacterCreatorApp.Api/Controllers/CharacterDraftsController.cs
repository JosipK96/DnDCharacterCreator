using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells;
using CharacterCreatorApp.Application.Features.CharacterCreation.CreateDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.DeleteDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetIncompleteDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetClass;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetRace;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CharacterCreatorApp.Api.Controllers
{
    [Authorize]
    [Route("api/character-drafts")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public class CharacterDraftsController(ISender sender, IMapper mapper) : ApiController
    {
        private readonly ISender _sender = sender;
        private readonly IMapper _mapper = mapper;

        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateEmptyDraft(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new CreateCharacterDraftCommand(), cancellationToken);

            return OkOrProblem(response);
        }

        [HttpPut("{draftId:guid}/name-and-level")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetNameAndLevel(Guid draftId, [FromBody] SetNameAndLevelApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterNameAndLevelCommand(draftId, request.Name, request.Level, request.Edition), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/race")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetRace(Guid draftId, [FromBody] SetRaceApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterRaceCommand(draftId, request.RaceId, request.SubraceId, request.AbilityIncreases), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/subrace")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetSubrace(Guid draftId, [FromBody] SetSubraceApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterSubraceCommand(draftId, request.SubraceId), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/class")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetClass(Guid draftId, [FromBody] SetClassApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterClassCommand(draftId, request.ClassId, request.SubclassId), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/subclass")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetSubclass(Guid draftId, [FromBody] SetSubclassApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterSubclassCommand(draftId, request.SubclassId), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/background")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetBackground(Guid draftId, [FromBody] SetBackgroundApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new SetCharacterBackgroundCommand(draftId, request.BackgroundId, request.AbilityIncreases ?? []), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/ability-scores")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetAbilityScores(Guid draftId, [FromBody] SetAbilityScoresApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterAbilityScoresCommand(draftId, request.Scores, request.Method), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/proficiencies")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseProficiencies(Guid draftId, [FromBody] ChooseProficienciesApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseProficienciesCommand(draftId, request.ProficiencyIds), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/race-skills")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseRaceSkills(Guid draftId, [FromBody] ChooseRaceSkillsApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseRaceSkillsCommand(draftId, request.ProficiencyIds), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/languages")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseLanguages(Guid draftId, [FromBody] ChooseLanguagesApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseLanguagesCommand(draftId, request.LanguageIds), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/ability-score-improvements")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetAbilityScoreImprovements(Guid draftId, [FromBody] SetAbilityScoreImprovementsApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetAbilityScoreImprovementsCommand(draftId, request.Improvements, request.FeatIds, request.FeatAbilityIncreases), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/spells")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseSpells(Guid draftId, [FromBody] ChooseSpellsApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseSpellsCommand(draftId, request.SpellIds), cancellationToken);

            return NoContentOrProblem(response);
        }

        /// <summary>
        /// The user's unfinished draft, so the wizard can resume it. 404 with
        /// CharacterDraft.NoIncompleteDraft when there is none, which the client treats as
        /// "start a new one".
        /// </summary>
        [HttpGet("incomplete")]
        [ProducesResponseType(typeof(CharacterDraftDetailsApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetIncompleteDraft(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetIncompleteCharacterDraftQuery(), cancellationToken);

            return OkOrProblem(response, draft => _mapper.Map<CharacterDraftDetailsApiResponse>(draft));
        }

        [HttpDelete("{draftId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDraft(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new DeleteCharacterDraftCommand(draftId), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpGet("{draftId:guid}")]
        [ProducesResponseType(typeof(CharacterDraftDetailsApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDraftById(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCharacterDraftByIdQuery(draftId), cancellationToken);

            return OkOrProblem(response, draft => _mapper.Map<CharacterDraftDetailsApiResponse>(draft));
        }

        /// <summary>
        /// Returns the character this draft would produce, without saving it.
        /// </summary>
        [HttpGet("{draftId:guid}/preview")]
        [ProducesResponseType(typeof(CharacterPreviewApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PreviewCharacter(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new PreviewCharacterQuery(draftId), cancellationToken);

            return OkOrProblem(response, preview => _mapper.Map<CharacterPreviewApiResponse>(preview));
        }
    }
}
