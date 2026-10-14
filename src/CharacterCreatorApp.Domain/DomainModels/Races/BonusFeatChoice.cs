namespace CharacterCreatorApp.Domain.DomainModels.Races;

/// <summary>
/// Feats a race gives the player to pick at level 1, beside the ones taken in place of ability
/// score improvements: Count of them, from Category ("Origin") or from any feat when that is null.
/// </summary>
public sealed record BonusFeatChoice(int Count, string? Category)
{
    /// <summary>Whether a feat of this category can fill one.</summary>
    public bool Accepts(string? featCategory) => Category == null || featCategory == Category;
}
