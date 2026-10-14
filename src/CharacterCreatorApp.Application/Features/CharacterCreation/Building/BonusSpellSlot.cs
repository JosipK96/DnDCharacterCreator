using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>One pick of spells a draft owes beside its class's: the race's (no FeatId), or one feat's.</summary>
public sealed record BonusSpellSlot(string? FeatId, string Name, BonusSpellChoice Choice);
