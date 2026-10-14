using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.DeleteDraft;

public record DeleteCharacterDraftCommand(Guid DraftId) : ICommand<Response>;