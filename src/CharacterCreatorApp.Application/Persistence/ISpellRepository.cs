using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence
{
    public interface ISpellRepository
    {
        Task<List<SpellReadModel>> GetAllAsync(CancellationToken cancellationToken);
        Task<SpellReadModel?> GetByIdAsync(string id, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetByClassIdAsync(string classId, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetByLevelAsync(int level, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetByClassIdAndLevelAsync(string classId, int level, CancellationToken cancellationToken);

        /// <summary>The spells a subclass adds to the list its class picks from, such as a 2014 warlock patron's.</summary>
        Task<List<SpellReadModel>> GetBySubclassIdAsync(string subclassId, CancellationToken cancellationToken);

        Task<List<SpellReadModel>> GetByMaxLevelAsync(int maxLevel, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetBySchoolAsync(string school, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetRitualSpellsAsync(CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetByClassIdAndMaxLevelAsync(string classId, int maxLevel, CancellationToken cancellationToken);
        Task<List<SpellReadModel>> GetByIdsAsync(List<string> ids, CancellationToken cancellationToken);
    }
}
