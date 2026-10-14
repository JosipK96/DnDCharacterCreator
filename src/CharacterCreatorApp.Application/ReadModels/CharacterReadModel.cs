namespace CharacterCreatorApp.Application.ReadModels;

public class CharacterReadModel
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? SubclassName { get; set; }
    public string RaceName { get; set; } = string.Empty;
    public string? SubraceName { get; set; }
    public string? BackgroundName { get; set; }
    public int Level { get; set; }
    public string Edition { get; set; } = string.Empty;
    public string RaceId { get; set; } = string.Empty;
    public string? SubraceId { get; set; }
    public string ClassId { get; set; } = string.Empty;
    public string? SubclassId { get; set; }
    public string BackgroundId { get; set; } = string.Empty;
    public Dictionary<string, int> AbilityScores { get; set; } = [];
    public List<string> ProficiencyIds { get; set; } = [];
    public List<string> LanguageIds { get; set; } = [];
    public List<string> SpellIds { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}