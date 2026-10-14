namespace CharacterCreatorApp.Domain.DomainModels.Feats;

/// <summary>An ability score a feat asks for, by canonical ability id ("STR").</summary>
public class AbilityMinimum
{
    public required string AbilityId { get; set; }
    public int Score { get; set; }
}
