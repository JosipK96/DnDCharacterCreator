using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetNameAndLevel;

/// <summary>The first step: the character's name, level and the rules edition it is built with.</summary>
public record SetCharacterNameAndLevelCommand(Guid DraftId, string Name, int Level, string Edition) : ICommand<Response>;
