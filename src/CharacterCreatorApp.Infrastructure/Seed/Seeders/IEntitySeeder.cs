namespace CharacterCreatorApp.Infrastructure.Seed.Seeders
{
    public interface IEntitySeeder
    {
        int Priority { get; }

        /// <summary>
        /// Reads this area's seed files and adds their rows to the context. SeedService has already
        /// deleted the old ones and saves afterwards.
        /// </summary>
        Task SeedAsync(ApplicationDbContext context, string dataPath, CancellationToken cancellationToken);
    }
}
