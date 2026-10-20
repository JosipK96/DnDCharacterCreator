using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Seed.Seeders;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharacterCreatorApp.Infrastructure.Seed
{
    /// <summary>
    /// Keeps the reference tables (races, classes, spells, feats and the rest) equal to the seed
    /// files. When the files differ from what the tables were filled from, every reference row is
    /// deleted and the files are read in again, in one transaction. Characters and drafts are not
    /// reference data: a draft loses only the choices the new data drops, and a character naming
    /// one keeps the old data in place.
    /// </summary>
    public class SeedService(
        ApplicationDbContext applicationDbContext,
        ILogger<SeedService> logger,
        IEnumerable<IEntitySeeder> seeders,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        // Any constant works, as long as nothing else in the database takes the same lock
        private const long ReplaceLockKey = 7_291_026_001;

        private readonly ApplicationDbContext _context = applicationDbContext;
        private readonly ILogger<SeedService> _logger = logger;
        private readonly IEnumerable<IEntitySeeder> _seeders = seeders;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly TimeProvider _timeProvider = timeProvider;

        // The build copies Infrastructure/Data next to the app. This used to walk from the
        // content root to the source folder, which only exists on a developer's machine.
        public static string DataPath => Path.Combine(AppContext.BaseDirectory, "Data");

        public Task SeedData(CancellationToken cancellationToken) => SeedData(DataPath, cancellationToken);

        public async Task SeedData(string dataPath, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Data path: {DataPath}", dataPath);

                var hash = await ReferenceDataHash.ComputeAsync(dataPath, cancellationToken);

                if (await IsCurrentAsync(hash, cancellationToken))
                {
                    _logger.LogInformation("Reference data is up to date.");
                    return;
                }

                await ReplaceAsync(dataPath, hash, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed database.");
                throw;
            }
        }

        private async Task ReplaceAsync(string dataPath, string hash, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Two instances starting together would both replace the data; the second waits here
            // and then finds the work done
            await _context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({ReplaceLockKey})", cancellationToken);

            if (await IsCurrentAsync(hash, cancellationToken))
            {
                return;
            }

            _logger.LogInformation("Seed files changed; replacing the reference data.");

            // A character's race, class, subclass and background are foreign keys. Deferred, they
            // are checked at commit, when the same ids are back; data that dropped one a character
            // still uses fails the commit and leaves everything as it was.
            await _context.Database.ExecuteSqlRawAsync("SET CONSTRAINTS ALL DEFERRED", cancellationToken);

            await DeleteReferenceDataAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            foreach (var seeder in _seeders.OrderBy(s => s.Priority))
            {
                await seeder.SeedAsync(_context, dataPath, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var sources = await ReferenceSources.LoadAsync(_context, cancellationToken);

            // A finished sheet is not changed behind the player's back: one character naming
            // something the new data drops keeps the old data, and the next start tries again
            if (await HasCharactersWithMissingReferencesAsync(sources, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Reference data not replaced: characters use entities the new seed files drop. The old data stays.");
                return;
            }

            await ClearMissingDraftChoicesAsync(sources, cancellationToken);
            await RecordVersionAsync(hash, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Reference data replaced.");
        }

        // shortcut: loads every character and draft, fine for one group's database; filter in SQL if it grows
        private async Task<bool> HasCharactersWithMissingReferencesAsync(ReferenceSources sources, CancellationToken cancellationToken)
        {
            var characters = await _context.Characters
                .AsNoTracking()
                .Select(c => new { c.Id, c.RaceId, c.SubraceId, c.ClassId, c.SubclassId, c.BackgroundId })
                .ToListAsync(cancellationToken);

            var blocked = false;

            foreach (var c in characters)
            {
                var missing = new[]
                    {
                        sources.Races.ContainsKey(c.RaceId) ? null : $"race {c.RaceId}",
                        c.SubraceId == null || sources.Subraces.ContainsKey(c.SubraceId) ? null : $"subrace {c.SubraceId}",
                        sources.Classes.ContainsKey(c.ClassId) ? null : $"class {c.ClassId}",
                        c.SubclassId == null || sources.Subclasses.ContainsKey(c.SubclassId) ? null : $"subclass {c.SubclassId}",
                        sources.Backgrounds.ContainsKey(c.BackgroundId) ? null : $"background {c.BackgroundId}",
                    }
                    .OfType<string>()
                    .ToList();

                if (missing.Count != 0)
                {
                    blocked = true;
                    _logger.LogError("Character {CharacterId} uses what the new seed files drop: {Missing}", c.Id, string.Join(", ", missing));
                }
            }

            return blocked;
        }

        private async Task ClearMissingDraftChoicesAsync(ReferenceSources sources, CancellationToken cancellationToken)
        {
            var drafts = await _context.CharacterDrafts.ToListAsync(cancellationToken);

            foreach (var draft in drafts)
            {
                var choiceSources = sources.For(draft);
                var missing = MissingIds(draft, choiceSources);

                if (missing.Count == 0)
                {
                    continue;
                }

                draft.ClearMissingChoices(choiceSources);
                _logger.LogWarning("Draft {DraftId} lost the choices the new seed files drop: {Missing}", draft.Id, string.Join(", ", missing));
            }
        }

        private static List<string> MissingIds(CharacterDraft draft, DraftChoiceSources choiceSources)
        {
            var feats = draft.SelectedFeatIds.Concat(draft.FightingStyleIds).Concat(draft.BackgroundFeatId is { } featId ? [featId] : []);
            var spells = draft.SelectedSpellIds.Concat(draft.RaceSpellIds).Concat(draft.FeatSpells.SelectMany(pick => pick.SpellIds));

            return new[]
                {
                    (draft.RaceId, choiceSources.Race),
                    (draft.SubraceId, choiceSources.Subrace),
                    (draft.ClassId, choiceSources.Class),
                    (draft.SubclassId, choiceSources.Subclass),
                    (draft.BackgroundId, choiceSources.Background),
                }
                .Where(choice => choice.Item1 != null && choice.Item2 == null)
                .Select(choice => choice.Item1!)
                .Concat(feats.Where(id => !choiceSources.Feats.ContainsKey(id)))
                .Concat(spells.Where(id => !choiceSources.Spells.ContainsKey(id)))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Deletes the root tables; the join and child tables go with them by cascade. Features
        /// first, because a feature's class and subclass keys are restricted rather than cascaded,
        /// and deleting a feature takes its background along.
        /// </summary>
        private async Task DeleteReferenceDataAsync(CancellationToken cancellationToken)
        {
            await _context.Features.ExecuteDeleteAsync(cancellationToken);
            await _context.Backgrounds.ExecuteDeleteAsync(cancellationToken);
            await _context.Spells.ExecuteDeleteAsync(cancellationToken);
            await _context.Subclasses.ExecuteDeleteAsync(cancellationToken);
            await _context.Classes.ExecuteDeleteAsync(cancellationToken);
            await _context.Subraces.ExecuteDeleteAsync(cancellationToken);
            await _context.Races.ExecuteDeleteAsync(cancellationToken);
            await _context.Feats.ExecuteDeleteAsync(cancellationToken);
            await _context.Languages.ExecuteDeleteAsync(cancellationToken);
            await _context.Proficiencies.ExecuteDeleteAsync(cancellationToken);
        }

        private async Task RecordVersionAsync(string hash, CancellationToken cancellationToken)
        {
            var version = await _context.ReferenceDataVersions.FindAsync([ReferenceDataVersion.SingletonId], cancellationToken);

            if (version == null)
            {
                version = new ReferenceDataVersion { Hash = hash };
                await _context.ReferenceDataVersions.AddAsync(version, cancellationToken);
            }

            version.Hash = hash;
            version.AppliedAt = _timeProvider.GetUtcNow();
        }

        private Task<bool> IsCurrentAsync(string hash, CancellationToken cancellationToken) =>
            _context.ReferenceDataVersions.AnyAsync(v => v.Hash == hash, cancellationToken);
    }
}
