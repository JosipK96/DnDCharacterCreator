using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class FeatureRepository(ApplicationDbContext dbContext) : IFeatureRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<FeatureReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Features
                .AsReadOnly()
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<FeatureReadModel>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Features
                .AsReadOnly()
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<FeatureReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken)
        {
            return await _dbContext.Features
                .AsNoTracking()
                .Where(f => ids.Contains(f.Id))
                .Select(f => f.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}
