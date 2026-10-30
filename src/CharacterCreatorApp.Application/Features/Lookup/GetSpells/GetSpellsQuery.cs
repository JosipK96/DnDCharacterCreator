using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSpells;

/// <summary>The spells a class picks from, or the ones a subclass adds to its class's list (the class wins when both are given); MaxLevel and Sources, when given, keep only spells up to that level and from those books.</summary>
public record GetSpellsQuery(
    string? ClassId = null,
    string? SubclassId = null,
    int? MaxLevel = null,
    IReadOnlyCollection<string>? Sources = null
) : IQuery<Response<List<SpellReadModel>>>;
