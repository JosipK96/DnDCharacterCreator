using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class ProficiencyRepository(ApplicationDbContext dbContext) : IProficiencyRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<ProficiencyReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Proficiencies
                .AsReadOnly()
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ProficiencyReadModel>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Proficiencies
                .AsReadOnly()
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ProficiencyReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken)
        {
            return await _dbContext.Proficiencies
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => p.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}