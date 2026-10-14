using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseFightingStyles;

/// <summary>The Fighting Style feats picked for the class's Fighting Style features.</summary>
public record ChooseFightingStylesCommand(Guid DraftId, List<string> FeatIds) : ICommand<Response>;
