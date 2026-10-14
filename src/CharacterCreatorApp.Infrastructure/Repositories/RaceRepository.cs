using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class RaceRepository(ApplicationDbContext dbContext) : IRaceRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        // Every query that loads a whole race starts here. Each used to spell out its own
        // includes, and none of them had RaceLanguages, so the lookup, the language step and
        // the built character all treated every race as knowing no languages.
        private IQueryable<Race> RacesWithDetails => _dbContext.Races
            .Include(r => r.AbilityBonuses)
            .Include(r => r.Traits)
            .Include(r => r.RaceLanguages)
            .Include(r => r.RaceProficiencies)
                .ThenInclude(rp => rp.Proficiency)
            .Include(r => r.Spells)
            .Include(r => r.Subraces)
                .ThenInclude(sr => sr.AbilityBonuses)
            .Include(r => r.Subraces)
                .ThenInclude(sr => sr.Traits)
            .Include(r => r.Subraces)
                .ThenInclude(sr => sr.SubraceProficiencies)
                    .ThenInclude(sp => sp.Proficiency)
            .Include(r => r.Subraces)
                .ThenInclude(sr => sr.Spells)
            .AsReadOnly();

        public async Task<RaceReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await RacesWithDetails
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<RaceReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken)
        {
            return await RacesWithDetails
                .Where(e => edition == null || e.Edition == edition)
                .OrderBy(e => e.Name)
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }

        public async Task<Race?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken)
        {
            return await RacesWithDetails
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<Race?> GetByIdWithSubracesAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Races
                .AsNoTracking()
                .Include(x => x.Subraces)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }
    }
}
