namespace CharacterCreatorApp.Infrastructure.Seed.Rows;

/// <summary>
/// One entry of Books.json, written by the generator: a book some race, class, background, feat
/// or spell comes from, by the code their Source holds ("PHB", "XGE").
/// </summary>
public sealed record BookSeedRow
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Edition { get; init; }

    /// <summary>True for the edition's Player's Handbook: PHB for 2014, XPHB for 2024.</summary>
    public bool IsPlayersHandbook { get; init; }
}
