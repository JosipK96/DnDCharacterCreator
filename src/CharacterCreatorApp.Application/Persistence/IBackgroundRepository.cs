using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IBackgroundRepository
    {
        Task<BackgroundReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);

        /// <summary>The backgrounds of one rules edition, or every background when edition is null.</summary>
        Task<List<BackgroundReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken);

        Task<Background?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken);
    }
}
