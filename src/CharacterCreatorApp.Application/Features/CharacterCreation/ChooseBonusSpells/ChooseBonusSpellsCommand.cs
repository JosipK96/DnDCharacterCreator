using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Domain.Common.Results;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.ChooseBonusSpells;

/// <summary>
/// The spells the race (a high elf's cantrip) and each feat with a spell choice (Magic Initiate)
/// let the player pick, one pick per feat in the character's order (see BonusSpells.FeatSlots),
/// and the ability picked for the race's spells where it offers one (a 2024 high elf's).
/// </summary>
public record ChooseBonusSpellsCommand(
    Guid DraftId, List<string> RaceSpellIds, List<FeatSpellPick> FeatSpells, string? RaceSpellAbilityId = null) : ICommand<Response>;
