using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetBonusSpells;

/// <summary>The race's pick first, if it has one, then one per feat with a spell choice, in the order ChooseBonusSpells takes them.</summary>
public record GetBonusSpellsQuery(Guid DraftId) : IQuery<Response<List<BonusSpellChoiceReadModel>>>;
