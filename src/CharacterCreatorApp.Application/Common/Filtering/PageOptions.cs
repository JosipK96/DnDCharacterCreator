namespace CharacterCreatorApp.Application.Common.Filtering;

/// <summary>
/// A page of a list query. An out-of-range index or size falls back to the default rather than
/// failing. The Api's PaginationParams forgives a client the same way; this is the application's
/// own copy of that rule, so it holds for any caller, not only the one that came over HTTP.
/// </summary>
public sealed class PageOptions
{
    public const int DefaultPageIndex = 0;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    private int _pageIndex = DefaultPageIndex;
    private int _pageSize = DefaultPageSize;

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 0 ? DefaultPageIndex : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > MaxPageSize ? DefaultPageSize : value;
    }

    public static PageOptions Default => new();
}
