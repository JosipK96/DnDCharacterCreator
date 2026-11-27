using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetHitPoints;

/// <summary>
/// The hit die's results, by level (CharacterDraft.HitDieLevels): rolled, or the fixed value. A
/// level-up names its new level; a new character or an edit names the levels from 2 it picked a
/// result for, and a level left out takes the fixed value.
/// </summary>
public record SetHitPointsCommand(Guid DraftId, IReadOnlyDictionary<int, int> Results) : ICommand<Response>;
