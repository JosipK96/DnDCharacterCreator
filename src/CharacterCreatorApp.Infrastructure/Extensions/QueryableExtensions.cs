using System.Linq.Expressions;

using CharacterCreatorApp.Application.Common.Filtering;

using Microsoft.EntityFrameworkCore;

namespace CharacterCreatorApp.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> AsReadOnly<T>(this IQueryable<T> queryable) where T : class
        => queryable.AsNoTracking().AsSplitQuery();

    /// <summary>
    /// Applies sorting and pagination to a query and returns one page of it
    /// </summary>
    public static async Task<PagedResult<T>> ApplySortingAndPaginationAsync<T>(
        this IQueryable<T> query,
        SortOptions? sorting,
        PageOptions? pagination,
        Expression<Func<T, object>> defaultSortSelector,
        string defaultSortDirection = "DESC",
        Action<SortingParamToEntityPropertyMapping<T>>? sortMappings = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        var sortedQuery = query.ApplySorting(
            sorting,
            defaultSortSelector,
            defaultSortDirection,
            sortMappings);

        // Apply pagination
        var page = pagination ?? PageOptions.Default;
        var items = await sortedQuery
            .Skip(page.PageIndex * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            items,
            totalCount,
            page.PageIndex,
            page.PageSize);
    }

    /// <summary>
    /// Applies sorting to a query
    /// </summary>
    public static IOrderedQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        SortOptions? sorting,
        Expression<Func<T, object>> defaultSortSelector,
        string defaultSortDirection = "DESC",
        Action<SortingParamToEntityPropertyMapping<T>>? sortMappings = null) where T : class
    {
        SortingParamToEntityPropertyMapping<T>? mapping = null;
        if (sortMappings != null)
        {
            mapping = new SortingParamToEntityPropertyMapping<T>();
            sortMappings(mapping);
        }

        // Determine sort selector and direction
        var sortSelector = defaultSortSelector;
        var sortDirection = defaultSortDirection;

        if (sorting?.SortColumn != null && mapping != null)
        {
            var mappedSelector = mapping.GetMapping(sorting.SortColumn);
            if (mappedSelector != null)
            {
                sortSelector = mappedSelector;
                sortDirection = sorting.SortDirection;
            }
        }

        // Apply sorting
        return sortDirection == SortDirection.ASC.ToString()
            ? query.OrderBy(sortSelector)
            : query.OrderByDescending(sortSelector);
    }

    /// <summary>
    /// Applies pagination to a query
    /// </summary>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PageOptions pagination)
    {
        return query
            .Skip(pagination.PageIndex * pagination.PageSize)
            .Take(pagination.PageSize);
    }

    /// <summary>
    /// Maps sorting parameter names to entity property expressions
    /// </summary>
    public class SortingParamToEntityPropertyMapping<TEntity>
    {
        private readonly Dictionary<string, Expression<Func<TEntity, object>>> _mappings = new(StringComparer.OrdinalIgnoreCase);

        public void AddMapping(string sortingParamName, Expression<Func<TEntity, object>> entityProperty)
        {
            _mappings[sortingParamName] = entityProperty;
        }

        public Expression<Func<TEntity, object>>? GetMapping(string? sortingParamName)
        {
            if (string.IsNullOrWhiteSpace(sortingParamName))
            {
                return null;
            }

            return _mappings.TryGetValue(sortingParamName, out var expression)
                ? expression
                : null;
        }
    }
}
