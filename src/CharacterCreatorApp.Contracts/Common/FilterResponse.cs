namespace CharacterCreatorApp.Contracts.Common;

public class FilterResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = new List<T>().AsReadOnly();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;

    // Parameterless constructor for deserialization
    public FilterResponse()
    {
    }

    // Constructor for creating responses
    public FilterResponse(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items.ToList().AsReadOnly();
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}