using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

/// <summary>The hit die's result for the level a level-up reaches: rolled, or its fixed value.</summary>
public record SetHitPointsCommand(Guid DraftId, int Result) : ICommand<Response>;
