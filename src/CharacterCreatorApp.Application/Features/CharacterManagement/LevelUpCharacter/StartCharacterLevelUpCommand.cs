using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterManagement.LevelUpCharacter;

/// <summary>Opens a saved character one level up as a draft for the wizard; returns the draft's id.</summary>
public record StartCharacterLevelUpCommand(Guid CharacterId) : ICommand<Response<Guid>>;
