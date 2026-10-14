using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.SetRace;

/// <summary>
/// Sets the race and subrace, with the +1s picked for them where they offer a choice (a
/// half-elf's two, a variant human's two); null or empty for any other race.
/// </summary>
public record SetCharacterRaceCommand(Guid DraftId, string RaceId, string? SubraceId, Dictionary<string, int>? AbilityIncreases = null) : ICommand<Response>;