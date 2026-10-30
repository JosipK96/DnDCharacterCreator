using CharacterCreatorApp.Application.Common.Abstractions;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.Common.Results;

namespace CharacterCreatorApp.Application.Features.Lookup.GetSpells;

public class GetSpellsQueryHandler(ISpellRepository spellRepository) : IQueryHandler<GetSpellsQuery, Response<List<SpellReadModel>>>
{
    private readonly ISpellRepository _spellRepository = spellRepository;

    public async Task<Response<List<SpellReadModel>>> Handle(GetSpellsQuery query, CancellationToken cancellationToken)
    {
        List<SpellReadModel> spells;

        if (!string.IsNullOrEmpty(query.ClassId))
        {
            spells = query.MaxLevel.HasValue
                ? await _spellRepository.GetByClassIdAndMaxLevelAsync(query.ClassId, query.MaxLevel.Value, cancellationToken)
                : await _spellRepository.GetByClassIdAsync(query.ClassId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.SubclassId))
        {
            spells = await _spellRepository.GetBySubclassIdAsync(query.SubclassId, cancellationToken);

            if (query.MaxLevel.HasValue)
            {
                spells = spells.FindAll(s => s.Level <= query.MaxLevel.Value);
            }
        }
        else
        {
            return new List<SpellReadModel>();
        }

        if (query.Sources is { Count: > 0 } sources)
        {
            spells = spells.FindAll(s => sources.Contains(s.Source));
        }

        return spells;
    }
}