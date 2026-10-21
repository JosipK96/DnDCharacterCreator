using CharacterCreatorApp.Domain.DomainModels.Feats;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>A feat's skill or tool choice, and the id and name of the feat that asks for it.</summary>
public sealed record FeatProficiencySlot(string FeatId, string Name, FeatChoice Choice);
