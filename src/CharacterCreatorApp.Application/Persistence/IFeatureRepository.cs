using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IFeatureRepository
    {
        Task<FeatureReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<FeatureReadModel>> GetAllAsync(CancellationToken cancellationToken);
        Task<List<FeatureReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken);
    }
}
