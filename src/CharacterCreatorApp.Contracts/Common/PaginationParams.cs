namespace CharacterCreatorApp.Contracts.Common;

public sealed class PaginationParams
{
    private int _pageIndex;
    private int _pageSize = PaginationDefaults.DefaultPageSize;

    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 0 ? 0 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 100 ? PaginationDefaults.DefaultPageSize : value;
    }

    public static PaginationParams Default => new()
    {
        PageIndex = PaginationDefaults.DefaultPageIndex,
        PageSize = PaginationDefaults.DefaultPageSize
    };
}