using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class FeatRepository(ApplicationDbContext dbContext) : IFeatRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<List<FeatReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken)
        {
            return await _dbContext.Feats
                .AsNoTracking()
                .Where(f => edition == null || f.Edition == edition)
                .OrderBy(f => f.Name)
                .Select(f => f.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<FeatReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Feats
                .AsNoTracking()
                .Where(f => f.Id == id)
                .Select(f => f.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<FeatReadModel>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken)
        {
            var idList = ids.Distinct().ToList();

            if (idList.Count == 0)
            {
                return [];
            }

            return await _dbContext.Feats
                .AsNoTracking()
                .Where(f => idList.Contains(f.Id))
                .Select(f => f.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FeatReadModel>> GetByProficiencyIdAsync(string proficiencyId, CancellationToken cancellationToken)
        {
            return await _dbContext.Feats
                .AsNoTracking()
                .Where(f => f.GrantedProficiencyIds.Contains(proficiencyId))
                .Select(f => f.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}
