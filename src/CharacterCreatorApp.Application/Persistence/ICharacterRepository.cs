using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ICharacterRepository
    {
        /// <summary>
        /// Creates a new character
        /// </summary>
        Task Create(Character character, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a character by ID, filtered by UserId for authorization
        /// </summary>
        Task<Character?> GetByIdAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all characters for a specific user
        /// </summary>
        Task<List<Character>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a character by ID, filtered by UserId for authorization
        /// </summary>
        Task<bool> DeleteAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a character exists for a specific user
        /// </summary>
        Task<bool> ExistsAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a character by ID with all related details, filtered by UserId for authorization
        /// </summary>
        Task<CharacterDetailsReadModel?> GetByIdWithDetailsAsync(Guid characterId, Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets filtered characters for a specific user based on filter criteria
        /// </summary>
        Task<PagedResult<CharacterReadModel>> GetFilteredCharactersAsync(Guid userId, FilterOptions<FilterCharacterSearchParams> filter, CancellationToken cancellationToken = default);
    }
}
