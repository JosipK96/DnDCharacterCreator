using CharacterCreatorApp.Domain.DomainModels.Proficiencies;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Building;

/// <summary>A tool choice and the name of the race, background or class that offers it.</summary>
public sealed record ToolSlot(string Name, ToolChoice Choice);
