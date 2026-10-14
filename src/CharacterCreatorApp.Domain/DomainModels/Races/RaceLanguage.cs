namespace CharacterCreatorApp.Domain.DomainModels.Races;

public class RaceLanguage
{
    public required string RaceId { get; set; }
    public required string Language { get; set; }

    public Race Race { get; set; } = null!;
}