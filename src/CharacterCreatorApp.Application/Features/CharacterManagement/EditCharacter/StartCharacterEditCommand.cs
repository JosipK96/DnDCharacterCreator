using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.EditCharacter;

/// <summary>Opens a saved character as a draft for the wizard; returns the draft's id.</summary>
public record StartCharacterEditCommand(Guid CharacterId) : ICommand<Response<Guid>>;
