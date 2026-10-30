using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class SpellRepository(ApplicationDbContext dbContext) : ISpellRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<SpellReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Spells
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => s.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SpellReadModel>> GetByClassIdAsync(string classId, CancellationToken cancellationToken)
        {
            return await _dbContext.Spells
                .AsNoTracking()
                .Include(s => s.SpellClasses)
                .Where(s => s.SpellClasses.Any(sc => sc.ClassId == classId))
                .Select(s => s.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SpellReadModel>> GetBySubclassIdAsync(string subclassId, CancellationToken cancellationToken)
        {
            // Only what the subclass adds to its class's list; the spells it has always prepared
            // are not picked, so they are no part of what can be chosen
            return await _dbContext.Spells
                .AsNoTracking()
                .Include(s => s.SpellClasses)
                .Where(s => s.SpellSubclasses.Any(ss => ss.SubclassId == subclassId && !ss.IsAlwaysPrepared))
                .Select(s => s.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SpellReadModel>> GetByClassIdAndMaxLevelAsync(string classId, int maxLevel, CancellationToken cancellationToken)
        {
            return await _dbContext.Spells
                .AsNoTracking()
                .Include(s => s.SpellClasses)
                .Where(s => s.SpellClasses.Any(sc => sc.ClassId == classId) && s.Level <= maxLevel)
                .Select(s => s.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SpellReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken)
        {
            return await _dbContext.Spells
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .Select(s => s.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}
