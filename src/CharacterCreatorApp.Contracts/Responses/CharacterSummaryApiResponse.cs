namespace CharacterCreatorApp.Contracts.Responses;

public class CharacterSummaryApiResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? SubclassName { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public string? SubraceName { get; set; }
    public int Level { get; set; }
    public string Edition { get; set; } = string.Empty;
    public string? BackgroundName { get; set; }
    public DateTime CreatedAt { get; set; }
}