using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IFeatRepository
    {
        /// <summary>The feats of one rules edition, or every feat when edition is null.</summary>
        Task<List<FeatReadModel>> GetAllAsync(string? edition, CancellationToken cancellationToken);

        Task<FeatReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<FeatReadModel>> GetByProficiencyIdAsync(string proficiencyId, CancellationToken cancellationToken);
        Task<List<FeatReadModel>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken);
    }
}
