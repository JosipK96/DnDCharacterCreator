using CharacterCreatorApp.Application.ReadModels.Proficiencies;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IProficiencyRepository
    {
        Task<ProficiencyReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<ProficiencyReadModel>> GetAllAsync(CancellationToken cancellationToken);
        Task<List<ProficiencyReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken);
    }
}
