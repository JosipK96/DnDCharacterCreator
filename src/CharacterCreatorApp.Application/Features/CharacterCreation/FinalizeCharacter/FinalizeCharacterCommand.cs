using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;

public record FinalizeCharacterCommand(Guid DraftId) : ICommand<Response<Guid>>;