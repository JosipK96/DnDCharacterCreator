namespace CharacterCreatorApp.Application.Common.Filtering;

/// <summary>
/// What a list query asks for: search parameters, a sort and a page. The Api maps its
/// FilterRequest onto this, so a change to the wire shape stops at the controller instead of
/// reaching the repositories.
/// </summary>
public sealed class FilterOptions<T> where T : new()
{
    public T? SearchParams { get; set; }
    public SortOptions? Sorting { get; set; }
    public PageOptions? Pagination { get; set; }

    public static FilterOptions<T> Default => new()
    {
        SearchParams = new T(),
        Pagination = PageOptions.Default,
        Sorting = null
    };
}
