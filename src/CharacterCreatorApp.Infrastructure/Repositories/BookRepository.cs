using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Infrastructure.Seed;
using CharacterCreatorApp.Infrastructure.Seed.Rows;

namespace CharacterCreatorApp.Infrastructure.Repositories;

/// <summary>
/// The books in Books.json, read from the seed data rather than a table: a row's Source is a
/// plain code with no key to join on, and the generator writes the one list of books there.
/// </summary>
public class BookRepository : IBookRepository
{
    public async Task<List<BookReadModel>> GetAllForEditionAsync(string edition, CancellationToken cancellationToken)
    {
        var rows = await SeedFile.ReadListAsync<BookSeedRow>(Path.Combine(SeedService.DataPath, "Books.json"), cancellationToken);

        return rows
            .Where(b => b.Edition == edition)
            .OrderByDescending(b => b.IsPlayersHandbook)
            .ThenBy(b => b.Name, StringComparer.Ordinal)
            .Select(b => new BookReadModel { Code = b.Code, Name = b.Name, Edition = b.Edition, IsPlayersHandbook = b.IsPlayersHandbook })
            .ToList();
    }
}
