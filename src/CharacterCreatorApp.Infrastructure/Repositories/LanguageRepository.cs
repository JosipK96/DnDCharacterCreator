using System.Linq.Expressions;

using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Languages;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories;

public class LanguageRepository(ApplicationDbContext dbContext) : ILanguageRepository
{
    private static readonly Expression<Func<Language, LanguageReadModel>> ToReadModel =
        l => new LanguageReadModel { Id = l.Id, Name = l.Name, Type = l.Type };

    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<LanguageReadModel>> GetAllForEditionAsync(string edition, CancellationToken cancellationToken)
    {
        var source = Language.SourceFor(edition);

        return await _dbContext.Languages
            .AsNoTracking()
            .Where(l => l.Source == source)
            .OrderBy(l => l.Name)
            .Select(ToReadModel)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LanguageReadModel>> GetChoosableAsync(string edition, CancellationToken cancellationToken)
    {
        var source = Language.SourceFor(edition);
        var types = Language.ChoosableTypes(edition).ToArray();

        return await _dbContext.Languages
            .AsNoTracking()
            .Where(l => l.Source == source && types.Contains(l.Type))
            .OrderBy(l => l.Name)
            .Select(ToReadModel)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LanguageReadModel>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
    {
        var idList = ids.ToList();

        if (idList.Count == 0)
        {
            return [];
        }

        return await _dbContext.Languages
            .AsNoTracking()
            .Where(l => idList.Contains(l.Id))
            .OrderBy(l => l.Name)
            .Select(ToReadModel)
            .ToListAsync(cancellationToken);
    }
}
