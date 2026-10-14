using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class SubraceRepository(ApplicationDbContext dbContext) : ISubraceRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<SubraceReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _dbContext.Subraces
                .AsReadOnly()
                .Where(e => e.Id == id)
                .Select(e => e.ToReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<SubraceReadModel>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Subraces
                .AsReadOnly()
                .Select(e => e.ToReadModel())
                .ToListAsync(cancellationToken);
        }
    }
}
