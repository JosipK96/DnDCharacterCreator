namespace CharacterCreatorApp.Application.ReadModels;

/// <summary>
/// Spells the draft's race or one of its feats lets the player pick: how many cantrips and
/// 1st-level spells, and the lists they may come from, each with its spells from the allowed books.
/// </summary>
public class BonusSpellChoiceReadModel
{
    /// <summary>The feat's id; null for the race's pick.</summary>
    public string? FeatId { get; set; }

    public required string Name { get; set; }
    public int Cantrips { get; set; }
    public int FirstLevelSpells { get; set; }
    public List<BonusSpellListReadModel> Lists { get; set; } = [];
}

public class BonusSpellListReadModel
{
    public required string ClassId { get; set; }
    public required string ClassName { get; set; }
    public List<SpellReadModel> Spells { get; set; } = [];
}
