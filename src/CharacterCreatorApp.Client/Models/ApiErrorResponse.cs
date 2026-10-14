namespace CharacterCreatorApp.Client.Models;

/// <summary>The problem body the API sends with every error response.</summary>
public class ApiErrorResponse
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public List<ApiError> Errors { get; set; } = [];
    public string? TraceId { get; set; }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string Detail { get; set; } = string.Empty;
}
