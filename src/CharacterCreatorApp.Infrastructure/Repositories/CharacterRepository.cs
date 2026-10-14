using System.Diagnostics.CodeAnalysis;

using CharacterCreatorApp.Application.Common.Filtering;
using CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Infrastructure.Extensions;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Repositories
{
    public class CharacterRepository(ApplicationDbContext dbContext) : ICharacterRepository
    {
        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task Create(Character character, CancellationToken cancellationToken)
        {
            await _dbContext.Characters.AddAsync(character, cancellationToken);
        }

        public async Task<Character?> GetByIdAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Characters.FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);
        }

        public async Task<List<Character>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Characters
                 .Where(c => c.UserId == userId)
                 .OrderByDescending(c => c.Level)
                 .ThenBy(c => c.Name)
                 .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
        {
            var character = await _dbContext.Characters.FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);

            if (character == null)
            {
                return false;
            }

            _dbContext.Characters.Remove(character);
            return true;
        }

        public async Task<bool> ExistsAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Characters.AnyAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);
        }

        public async Task<CharacterDetailsReadModel?> GetByIdWithDetailsAsync(Guid characterId, Guid userId, CancellationToken cancellationToken)
        {
            var character = await _dbContext.Characters
                .Include(c => c.Class)
                .Include(c => c.Subclass)
                .Include(c => c.Race)
                .Include(c => c.Subrace)
                .Include(c => c.Background)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == characterId && c.UserId == userId, cancellationToken);

            if (character == null)
            {
                return null;
            }

            return character.ToDetailsReadModel();
        }

        public async Task<PagedResult<CharacterReadModel>> GetFilteredCharactersAsync(Guid userId, FilterOptions<FilterCharacterSearchParams> filter, CancellationToken cancellationToken)
        {
            var query = ApplyFilter(userId, filter.SearchParams);

            var response = await query.ApplySortingAndPaginationAsync(
                sorting: filter.Sorting,
                pagination: filter.Pagination,
                defaultSortSelector: c => c.CreatedAt,
                defaultSortDirection: SortDirection.DESC.ToString(),
                sortMappings: options =>
                {
                    options.AddMapping("name", c => c.Name);
                    options.AddMapping("level", c => c.Level);
                    options.AddMapping("createdat", c => c.CreatedAt);
                },
                cancellationToken: cancellationToken);

            return new PagedResult<CharacterReadModel>(
                response.Items.Select(c => c.ToReadModel()),
                response.TotalCount,
                response.PageIndex,
                response.PageSize);
        }

        // ToLower() is what EF translates to SQL lower(). ToLowerInvariant() and the
        // StringComparison overloads have no translation, so applying those suggestions here
        // would move the comparison out of the database and into memory.
        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "Must stay translatable to SQL")]
        [SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "Must stay translatable to SQL")]
        [SuppressMessage("Globalization", "CA1862:Prefer the StringComparison method overloads", Justification = "Must stay translatable to SQL")]
        private IQueryable<Character> ApplyFilter(Guid userId, FilterCharacterSearchParams? searchParams)
        {
            var query = _dbContext.Characters
                .Include(c => c.Class)
                .Include(c => c.Subclass)
                .Include(c => c.Race)
                .Include(c => c.Subrace)
                .Include(c => c.Background)
                .Where(c => c.UserId == userId)
                .AsNoTracking();

            if (searchParams == null)
            {
                return query;
            }

            if (!string.IsNullOrWhiteSpace(searchParams.Name))
            {
                query = query.Where(c => c.Name.Contains(searchParams.Name));
            }

            if (!string.IsNullOrWhiteSpace(searchParams.ClassId))
            {
                query = query.Where(c => c.ClassId.ToLower() == searchParams.ClassId.ToLower());
            }

            if (searchParams.MinLevel.HasValue)
            {
                query = query.Where(c => c.Level >= searchParams.MinLevel.Value);
            }

            if (searchParams.MaxLevel.HasValue)
            {
                query = query.Where(c => c.Level <= searchParams.MaxLevel.Value);
            }

            return query;
        }
    }
}
