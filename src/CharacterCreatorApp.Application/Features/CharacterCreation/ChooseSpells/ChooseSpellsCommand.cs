using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseSpells;

public record ChooseSpellsCommand(Guid DraftId, List<string> SpellIds) : ICommand<Response>;