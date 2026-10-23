using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ICharacterDraftRepository
    {
        Task<CharacterDraft?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// The draft to change or delete, only while it is unfinished: a completed draft is a
        /// character's build, which the wizard's steps and finalize must never change.
        /// </summary>
        Task<CharacterDraft?> GetByIdForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken);

        /// <summary>The completed draft holding every choice the character has now, if one is kept.</summary>
        Task<CharacterDraft?> GetBuildAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        /// <summary>The unfinished edit of the character, if one was left unsaved.</summary>
        Task<CharacterDraft?> GetEditAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        Task CreateAsync(CharacterDraft draft, CancellationToken cancellationToken);
        Task UpdateAsync(CharacterDraft draft, CancellationToken cancellationToken);
        Task DeleteAsync(CharacterDraft draft, CancellationToken cancellationToken);

        /// <summary>The user's unfinished new character, to resume; an edit of a saved one is never it.</summary>
        Task<CharacterDraftDetailsReadModel?> GetIncompleteDraft(Guid userId, CancellationToken cancellationToken);

        Task<CharacterDraftDetailsReadModel?> GetDetailsById(Guid draftId, Guid userId, CancellationToken cancellationToken);
    }
}
