using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.DeleteCharacter;

public record DeleteCharacterCommand(Guid CharacterId) : ICommand<Response>;