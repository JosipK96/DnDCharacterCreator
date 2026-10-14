namespace CharacterCreatorApp.Application.Features.CharacterManagement.GetMyCharacters;

public class FilterCharacterSearchParams
{
    public string? Name { get; set; }
    public string? ClassId { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
}