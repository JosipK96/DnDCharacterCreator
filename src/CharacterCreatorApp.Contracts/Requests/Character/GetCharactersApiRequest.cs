namespace CharacterCreatorApp.Contracts.Requests.Character;

public class GetCharactersApiRequest
{
    public string? Name { get; set; }
    public string? ClassId { get; set; }
    public int? MinLevel { get; set; }
    public int? MaxLevel { get; set; }
}