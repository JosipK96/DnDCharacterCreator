using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Races;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IRaceRepository
    {
        Task<RaceReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);

        /// <summary>The races of one rules edition, or every race when edition is null.</summary>
        Task<List<RaceReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken);

        Task<Race?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken);
        Task<Race?> GetByIdWithSubracesAsync(string id, CancellationToken cancellationToken);
    }
}
