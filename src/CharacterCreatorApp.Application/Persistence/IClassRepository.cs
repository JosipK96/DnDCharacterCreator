using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Classes;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface IClassRepository
    {
        Task<ClassReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);

        /// <summary>The classes of one rules edition, or every class when edition is null.</summary>
        Task<List<ClassReadModel>> GetClassesAsync(string? edition, CancellationToken cancellationToken);

        Task<Class?> GetByIdWithDetailsAsync(string id, CancellationToken cancellationToken);
        Task<Class?> GetByIdWithSubclassesAsync(string id, CancellationToken cancellationToken);
    }
}
