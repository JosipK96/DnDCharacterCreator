using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.CharacterCreation.GetSpellAllowance;

/// <summary>How many cantrips and spells the draft's class lets it pick, as ChooseSpells checks them.</summary>
public record GetSpellAllowanceQuery(Guid DraftId) : IQuery<Response<SpellAllowanceReadModel>>;
