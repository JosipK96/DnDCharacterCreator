using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetSubclass;

public record SetCharacterSubclassCommand(Guid DraftId, string SubclassId) : ICommand<Response>;