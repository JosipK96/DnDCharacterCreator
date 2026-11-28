using CharacterCreatorApp.Application.ReadModels.Spells;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.Shared.Choices;

/// <summary>
/// What <see cref="SpellChoice.LoadStepListAsync"/> builds: the spells step's list, and the spells
/// it would offer to pick from the books the draft has not ticked.
/// </summary>
public sealed record StepSpellList(List<SpellReadModel> Listed, List<SpellReadModel> InUntickedBooks);
