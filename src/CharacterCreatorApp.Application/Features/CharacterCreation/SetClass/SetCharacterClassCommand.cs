using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetClass;

public record SetCharacterClassCommand(Guid DraftId, string ClassId, string? SubclassId) : ICommand<Response>;