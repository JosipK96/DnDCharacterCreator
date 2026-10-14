using CharacterCreatorApp.Application.ReadModels;

namespace CharacterCreatorApp.Application.Persistence;

public interface IBookRepository
{
    /// <summary>The books of one rules edition, the Player's Handbook first, then by name.</summary>
    Task<List<BookReadModel>> GetAllForEditionAsync(string edition, CancellationToken cancellationToken);
}
