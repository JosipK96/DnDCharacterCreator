using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetBackground;

/// <summary>
/// Sets the background, with a 2024 one's ability increases (empty for a 2014 one) and the feat
/// picked for one that lets the player choose it (a Mist Wanderer's Dark Gift).
/// </summary>
public record SetCharacterBackgroundCommand(Guid DraftId, string BackgroundId, Dictionary<string, int> AbilityIncreases, string? FeatId = null) : ICommand<Response>;
