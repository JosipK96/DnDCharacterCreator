using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSpells;

public record GetSpellsQuery(
    string? ClassId = null,
    string? SubclassId = null,
    int? Level = null,
    int? MaxLevel = null,
    string? School = null,
    bool? IsRitual = null
) : IQuery<Response<List<SpellReadModel>>>;