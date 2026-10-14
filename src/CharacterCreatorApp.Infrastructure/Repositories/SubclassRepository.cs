using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class SubclassRepository(ApplicationDbContext dbContext) : ISubclassRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        // Every query starts here. The lookup by id left the spell table out, so the spells step
        // found no spellcasting for an Eldritch Knight or Arcane Trickster.
        private IQueryable<Subclass> SubclassesWithDetails => _dbContext.Subclasses
            .Include(s => s.SpellProgression)
            .AsReadOnly();

        public async Task<SubclassReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await SubclassesWithDetails
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SubclassReadModel>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await SubclassesWithDetails
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SubclassReadModel>> GetByClassIdAsync(string classId, CancellationToken cancellationToken)
        {
            return await SubclassesWithDetails
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.Name)
                .Select(s => s.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}
