namespace CharacterCreatorApp.Application.Common.Filtering;

public sealed class SortOptions
{
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; } = "DESC";
}
