namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>How many Fighting Styles the draft's character picks, and the ones its class offers.</summary>
public class FightingStylesReadModel
{
    public int Owed { get; set; }
    public List<FeatReadModel> Options { get; set; } = [];
}
