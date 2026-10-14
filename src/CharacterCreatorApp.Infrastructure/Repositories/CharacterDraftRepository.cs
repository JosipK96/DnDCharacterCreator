using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class CharacterDraftRepository(ApplicationDbContext dbContext) : ICharacterDraftRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<CharacterDraft?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.CharacterDrafts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id && d.CreatedByUserId == userId, cancellationToken);
        }

        public async Task<CharacterDraft?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.CharacterDrafts
                .Where(x => x.Id == id && x.CreatedByUserId == userId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task CreateAsync(CharacterDraft draft, CancellationToken cancellationToken)
        {
            await _dbContext.CharacterDrafts.AddAsync(draft, cancellationToken);
        }

        public Task UpdateAsync(CharacterDraft draft, CancellationToken cancellationToken)
        {
            _dbContext.CharacterDrafts.Update(draft);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CharacterDraft draft, CancellationToken cancellationToken)
        {
            _dbContext.CharacterDrafts.Remove(draft);
            return Task.CompletedTask;
        }

        public async Task<CharacterDraftDetailsReadModel?> GetIncompleteDraft(Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.CharacterDrafts
                .AsNoTracking()
                .Where(d => !d.IsCompleted && d.CreatedByUserId == userId)
                .Select(d => d.ToDetailsReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<CharacterDraftDetailsReadModel?> GetDetailsById(Guid draftId, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.CharacterDrafts
                .AsNoTracking()
                .Where(d => d.Id == draftId && d.CreatedByUserId == userId)
                .Select(d => d.ToDetailsReadModel())
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
