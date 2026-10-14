namespace CharacterCreatorApp.Contracts.Common;

public class FilterRequest<T> where T : new()
{
    public T? SearchParams { get; set; }
    public SortingParams? Sorting { get; set; }
    public PaginationParams? Pagination { get; set; }
    public static FilterRequest<T> Default => new()
    {
        SearchParams = new T(),
        Pagination = PaginationParams.Default,
        Sorting = null
    };
}