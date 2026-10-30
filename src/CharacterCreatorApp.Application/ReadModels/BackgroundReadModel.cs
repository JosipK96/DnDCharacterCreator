namespace CharacterCreatorApp.Application.ReadModels;

public class BackgroundReadModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024.</summary>
    public string Edition { get; set; } = string.Empty;

    /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>The languages the background names; the ones the player picks are counted in LanguageChoiceCount.</summary>
    public List<string> Languages { get; set; } = new();
    public int LanguageChoiceCount { get; set; }
    public List<BackgroundProficiencyReadModel> Proficiencies { get; set; } = new();

    /// <summary>The three abilities a 2024 background raises, as ids ("STR"); empty for a 2014 one.</summary>
    public List<string> AbilityScoreOptions { get; set; } = new();

    /// <summary>The feat the background gives, or null.</summary>
    public BackgroundFeatReadModel? Feat { get; set; }

    /// <summary>The category the player picks the feat from when the background has none of its own ("Dark Gift").</summary>
    public string? FeatChoiceCategory { get; set; }

    /// <summary>The category the player may pick a feat from in place of Feat ("Dark Gift" for a Haunted One).</summary>
    public string? FeatAlternativeCategory { get; set; }

    /// <summary>A 2014 background's feature; null for a 2024 one.</summary>
    public BackgroundFeatureReadModel? Feature { get; set; }
}

public class BackgroundProficiencyReadModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class BackgroundFeatReadModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRepeatable { get; set; }
}

public class BackgroundFeatureReadModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
