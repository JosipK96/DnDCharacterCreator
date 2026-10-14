using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class ClassRepository(ApplicationDbContext dbContext) : IClassRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        // Every query that loads a whole class starts here, so a class reads the same from the
        // list, by id and in the character builder. The list used to leave out spellcasting.
        private IQueryable<Class> ClassesWithDetails => _dbContext.Classes
            .Include(c => c.ClassProficiencies)
                .ThenInclude(cp => cp.Proficiency)
            .Include(c => c.ProficiencyChoice)
                .ThenInclude(pc => pc!.Options)
                    .ThenInclude(opt => opt.Proficiency)
            .Include(c => c.Features)
            .Include(c => c.Subclasses)
                .ThenInclude(sc => sc.Features)
            .Include(c => c.Subclasses)
                .ThenInclude(sc => sc.Spells)
            .Include(c => c.Subclasses)
                .ThenInclude(sc => sc.SpellProgression)
            .Include(c => c.Spellcasting)
            .Include(c => c.SpellProgression)
            .AsReadOnly();

        public async Task<ClassReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await ClassesWithDetails
                .Where(c => c.Id == id)
                .Select(c => c.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ClassReadModel>> GetClassesAsync(string? edition, CancellationToken cancellationToken)
        {
            return await ClassesWithDetails
                .Where(c => edition == null || c.Edition == edition)
                .OrderBy(c => c.Name)
                .Select(c => c.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<Class?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken)
        {
            return await ClassesWithDetails
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Class?> GetByIdWithSubclassesAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Classes
                .AsNoTracking()
                .Include(x => x.Subclasses)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
