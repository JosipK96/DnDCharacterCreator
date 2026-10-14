using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ISubraceRepository
    {
        Task<SubraceReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<SubraceReadModel>> GetAllAsync(CancellationToken cancellationToken);
    }
}
