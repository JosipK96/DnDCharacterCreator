using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.CreateDraft;

public record CreateCharacterDraftCommand : ICommand<Response<Guid>>;