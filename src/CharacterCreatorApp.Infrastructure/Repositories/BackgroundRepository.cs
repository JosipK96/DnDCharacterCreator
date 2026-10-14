using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class BackgroundRepository(ApplicationDbContext dbContext) : IBackgroundRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        // Every query that loads a whole background starts here, so the list, the lookup by id
        // and the character builder cannot load different parts of it.
        private IQueryable<Background> BackgroundsWithDetails => _dbContext.Backgrounds
            .Include(b => b.Feature)
            .Include(b => b.Feat)
            .Include(b => b.BackgroundProficiencies)
                .ThenInclude(bp => bp.Proficiency)
            .AsReadOnly();

        public async Task<BackgroundReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await BackgroundsWithDetails
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<BackgroundReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken)
        {
            return await BackgroundsWithDetails
                .Where(e => edition == null || e.Edition == edition)
                .OrderBy(e => e.Name)
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<Background?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken)
        {
            return await BackgroundsWithDetails
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }
    }
}
