using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ICharacterDraftRepository
    {
        Task<CharacterDraft?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);
        Task<CharacterDraft?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken);
        Task CreateAsync(CharacterDraft draft, CancellationToken cancellationToken);
        Task UpdateAsync(CharacterDraft draft, CancellationToken cancellationToken);
        Task DeleteAsync(CharacterDraft draft, CancellationToken cancellationToken);
        Task<CharacterDraftDetailsReadModel?> GetIncompleteDraft(Guid userId, CancellationToken cancellationToken);
        Task<CharacterDraftDetailsReadModel?> GetDetailsById(Guid draftId, Guid userId, CancellationToken cancellationToken);
    }
}
