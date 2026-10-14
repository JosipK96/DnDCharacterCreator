using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubrace;

public record SetCharacterSubraceCommand(Guid DraftId, string? SubraceId) : ICommand<Response>;