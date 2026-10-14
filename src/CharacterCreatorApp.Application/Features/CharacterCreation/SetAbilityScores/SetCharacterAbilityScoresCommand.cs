using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;

public record SetCharacterAbilityScoresCommand(Guid DraftId, Dictionary<string, int> Scores) : ICommand<Response>;