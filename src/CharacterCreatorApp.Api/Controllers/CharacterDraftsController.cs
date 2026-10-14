using CharacterCreatorApp.Api.Infrastructure.ApiProblemDetails;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseBonusSpells;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseFightingStyles;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseProficiencies;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseRaceSkills;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseTools;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells;
using CharacterCreatorApp.Application.Features.CharacterCreation.CreateDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.DeleteDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetBonusSpells;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetToolChoices;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetFightingStyles;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetIncompleteDraft;
using CharacterCreatorApp.Application.Features.CharacterCreation.PreviewCharacter;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAllowedBooks;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetClass;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetRace;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;
using CharacterCreatorApp.Contracts.Requests.CharacterDrafts;
using CharacterCreatorApp.Contracts.Responses;
using CharacterCreatorApp.Domain.DomainModels.Characters;

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

        /// <summary>
        /// The books the character may take its choices from, besides its edition's Player's
        /// Handbook. Choices from a book no longer allowed are cleared.
        /// </summary>
        [HttpPut("{draftId:guid}/books")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetAllowedBooks(Guid draftId, [FromBody] SetAllowedBooksApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetAllowedBooksCommand(draftId, request.Books), cancellationToken);

            return NoContentOrProblem(response);
        }

        [HttpPut("{draftId:guid}/race")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetRace(Guid draftId, [FromBody] SetRaceApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new SetCharacterRaceCommand(draftId, request.RaceId, request.SubraceId, request.AbilityIncreases, request.Size), cancellationToken);

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
                new SetCharacterBackgroundCommand(draftId, request.BackgroundId, request.AbilityIncreases ?? [], request.FeatId), cancellationToken);

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

        /// <summary>
        /// The tools picked for each tool choice the race, background and class offer, one list
        /// per choice in the order GET tool-choices lists them.
        /// </summary>
        [HttpPut("{draftId:guid}/tools")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseTools(Guid draftId, [FromBody] ChooseToolsApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseToolsCommand(draftId, request.Picks), cancellationToken);

            return NoContentOrProblem(response);
        }

        /// <summary>
        /// The tools the draft's race (a dwarf's artisan's tools), background (a Criminal's gaming
        /// set) and class (a bard's instruments) let the player pick, in that order; empty when none do.
        /// </summary>
        [HttpGet("{draftId:guid}/tool-choices")]
        [ProducesResponseType(typeof(List<ToolChoiceApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetToolChoices(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetToolChoicesQuery(draftId), cancellationToken);

            return OkOrProblem(response, choices => _mapper.Map<List<ToolChoiceApiResponse>>(choices));
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

        [HttpPut("{draftId:guid}/fighting-styles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseFightingStyles(Guid draftId, [FromBody] ChooseFightingStylesApiRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new ChooseFightingStylesCommand(draftId, request.FeatIds), cancellationToken);

            return NoContentOrProblem(response);
        }

        /// <summary>
        /// How many Fighting Styles the draft's character picks (0 for a class without the
        /// feature) and the ones its class offers.
        /// </summary>
        [HttpGet("{draftId:guid}/fighting-styles")]
        [ProducesResponseType(typeof(FightingStylesApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFightingStyles(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetFightingStylesQuery(draftId), cancellationToken);

            return OkOrProblem(response, styles => _mapper.Map<FightingStylesApiResponse>(styles));
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
        /// The spells the race and each feat with a spell choice let the player pick, one pick per
        /// feat in the order GET bonus-spells lists them.
        /// </summary>
        [HttpPut("{draftId:guid}/bonus-spells")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChooseBonusSpells(Guid draftId, [FromBody] ChooseBonusSpellsApiRequest request, CancellationToken cancellationToken)
        {
            var featSpells = request.FeatSpells.ConvertAll(pick => new FeatSpellPick { FeatId = pick.FeatId, ClassId = pick.ClassId, SpellIds = pick.SpellIds });
            var response = await _sender.Send(new ChooseBonusSpellsCommand(draftId, request.RaceSpellIds, featSpells), cancellationToken);

            return NoContentOrProblem(response);
        }

        /// <summary>
        /// The spells the draft's race (a high elf's cantrip) and feats (Magic Initiate) let the
        /// player pick, the race's first, each with the lists it may come from; empty when none do.
        /// </summary>
        [HttpGet("{draftId:guid}/bonus-spells")]
        [ProducesResponseType(typeof(List<BonusSpellChoiceApiResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CustomProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBonusSpells(Guid draftId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetBonusSpellsQuery(draftId), cancellationToken);

            return OkOrProblem(response, choices => _mapper.Map<List<BonusSpellChoiceApiResponse>>(choices));
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
