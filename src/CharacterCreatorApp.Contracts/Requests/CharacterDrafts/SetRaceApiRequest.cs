namespace CharacterCreatorApp.Contracts.Requests.CharacterDrafts;

public class SetRaceApiRequest
{
    public required string RaceId { get; set; }
    public string? SubraceId { get; set; }

    /// <summary>
    /// The increases picked where the race or subrace offers them, {"DEX": 1, "WIS": 1} for a
    /// half-elf, {"STR": 2, "WIS": 1} for a Monsters of the Multiverse race; none for any other.
    /// </summary>
    public Dictionary<string, int>? AbilityIncreases { get; set; }

    /// <summary>"Small" or "Medium" for a species that may be either (a 2024 human); null otherwise.</summary>
    public string? Size { get; set; }
}