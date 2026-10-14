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

        if (!string.IsNullOrEmpty(query.ClassId) && query.MaxLevel.HasValue)
        {
            spells = await _spellRepository.GetByClassIdAndMaxLevelAsync(query.ClassId, query.MaxLevel.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.ClassId) && query.Level.HasValue)
        {
            spells = await _spellRepository.GetByClassIdAndLevelAsync(query.ClassId, query.Level.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.ClassId))
        {
            spells = await _spellRepository.GetByClassIdAsync(query.ClassId, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.SubclassId))
        {
            spells = await _spellRepository.GetBySubclassIdAsync(query.SubclassId, cancellationToken);
        }
        else if (query.Level.HasValue)
        {
            spells = await _spellRepository.GetByLevelAsync(query.Level.Value, cancellationToken);
        }
        else if (query.MaxLevel.HasValue)
        {
            spells = await _spellRepository.GetByMaxLevelAsync(query.MaxLevel.Value, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(query.School))
        {
            spells = await _spellRepository.GetBySchoolAsync(query.School, cancellationToken);
        }
        else if (query.IsRitual == true)
        {
            spells = await _spellRepository.GetRitualSpellsAsync(cancellationToken);
        }
        else
        {
            spells = await _spellRepository.GetAllAsync(cancellationToken);
        }

        if (query.IsRitual.HasValue && query.IsRitual.Value)
        {
            spells = spells.Where(s => s.IsRitual).ToList();
        }

        if (!string.IsNullOrEmpty(query.School))
        {
            spells = spells.Where(s => s.School == query.School).ToList();
        }

        if (query.MaxLevel.HasValue && (!string.IsNullOrEmpty(query.ClassId) || !string.IsNullOrEmpty(query.SubclassId)))
        {
            spells = spells.Where(s => s.Level <= query.MaxLevel.Value).ToList();
        }

        if (query.Sources is { Count: > 0 } sources)
        {
            spells = spells.FindAll(s => sources.Contains(s.Source));
        }

        return spells;
    }
}