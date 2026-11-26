namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>
/// Whether the draft's character can take one feat, as the improvements step's save would decide
/// it. Unmet is the first prerequisite it falls short of for a feat taken for an improvement, and
/// UnmetAsRaceFeat for the race's own feat, which counts no improvement; null means it can.
/// CheckWithDm says the feat also asks for something the app does not check.
/// </summary>
public class FeatEligibilityReadModel
{
    public string FeatId { get; set; } = string.Empty;
    public string? Unmet { get; set; }
    public string? UnmetAsRaceFeat { get; set; }
    public bool CheckWithDm { get; set; }
}
