using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;

/// <summary>
/// Sets what the character does with each ability score improvement it has earned. Every
/// improvement may be spent either on ability points or on a feat, so the two lists are set
/// together and their combined count must match the improvements the character is entitled to.
/// The feats a race gives besides them (a variant human's, a 2024 human's Origin feat) come
/// first in FeatIds. FeatAbilityIncreases says what each feat raises, in the same order: {"DEX":
/// 1} for an Athlete who picked Dexterity, empty for a feat that raises nothing or a fixed
/// ability; null when no feat raises a picked one.
/// </summary>
public record SetAbilityScoreImprovementsCommand(
    Guid DraftId,
    List<Dictionary<string, int>> Improvements,
    List<string> FeatIds,
    List<Dictionary<string, int>>? FeatAbilityIncreases = null) : ICommand<Response>;
