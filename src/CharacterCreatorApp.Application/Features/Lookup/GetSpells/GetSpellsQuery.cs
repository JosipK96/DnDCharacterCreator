using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSpells;

/// <summary>Spells by class, subclass, level, school or ritual; Sources, when given, keeps only those from these books.</summary>
public record GetSpellsQuery(
    string? ClassId = null,
    string? SubclassId = null,
    int? Level = null,
    int? MaxLevel = null,
    string? School = null,
    bool? IsRitual = null,
    IReadOnlyCollection<string>? Sources = null
) : IQuery<Response<List<SpellReadModel>>>;