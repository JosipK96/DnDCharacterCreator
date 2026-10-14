using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScores;

/// <summary>The six base scores and how they were generated (AbilityScoreMethod).</summary>
public record SetCharacterAbilityScoresCommand(Guid DraftId, Dictionary<string, int> Scores, string Method) : ICommand<Response>;
