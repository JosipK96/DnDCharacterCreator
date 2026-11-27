using CharacterCreatorApp.Application.ReadModels.Classes;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ISubclassRepository
    {
        Task<SubclassReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<SubclassReadModel>> GetAllAsync(CancellationToken cancellationToken);
        Task<List<SubclassReadModel>> GetByClassIdAsync(string classId, CancellationToken cancellationToken);
    }
}
