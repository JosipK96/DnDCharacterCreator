namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>A book the reference data comes from, by the code a row's Source holds ("PHB", "XGE").</summary>
public sealed class BookReadModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024; no book is in both.</summary>
    public required string Edition { get; init; }

    /// <summary>True for the edition's Player's Handbook, which every character of it may use.</summary>
    public bool IsPlayersHandbook { get; init; }
}
