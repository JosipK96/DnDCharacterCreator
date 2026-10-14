namespace CharacterCreatorApp.Application.ReadModels;

public class SpellReadModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
    public string Source { get; set; } = string.Empty;

    public int Level { get; set; }
    public required string School { get; set; }
    public int CastingTimeNumber { get; set; }
    public required string CastingTimeUnit { get; set; }
    public required string RangeType { get; set; }
    public string? RangeDistanceType { get; set; }
    public int? RangeDistanceAmount { get; set; }
    public bool HasVerbalComponent { get; set; }
    public bool HasSomaticComponent { get; set; }
    public bool HasMaterialComponent { get; set; }
    public string? Material { get; set; }
    public required string DurationType { get; set; }
    public int? DurationAmount { get; set; }
    public string? DurationUnit { get; set; }
    public bool IsConcentration { get; set; }
    public bool IsRitual { get; set; }
    public required string Description { get; set; }

    public List<string> ClassIds { get; set; } = [];

    // The data stores casting time, range and duration in parts; these put them the way the
    // book prints them, once, for everything that shows a spell.

    /// <summary>"1 action", "1 bonus action", "10 minutes".</summary>
    public string CastingTime
    {
        get
        {
            var unit = CastingTimeUnit == "bonus" ? "bonus action" : CastingTimeUnit;
            return CastingTimeNumber == 1 ? $"1 {unit}" : $"{CastingTimeNumber} {unit}s";
        }
    }

    /// <summary>"60 feet", "Touch", "Self", or an area around the caster: "Self (15-foot cone)".</summary>
    public string Range => RangeType switch
    {
        "point" => RangeDistanceType switch
        {
            "feet" => $"{RangeDistanceAmount} feet",
            "miles" => RangeDistanceAmount == 1 ? "1 mile" : $"{RangeDistanceAmount} miles",
            "touch" => "Touch",
            "self" => "Self",
            "sight" => "Sight",
            "unlimited" => "Unlimited",
            _ => "Special",
        },
        "special" => "Special",
        _ => $"Self ({RangeDistanceAmount}-{(RangeDistanceType == "miles" ? "mile" : "foot")} {RangeType})",
    };

    /// <summary>"V, S, M (a sprig of mistletoe)".</summary>
    public string Components => string.Join(", ", new[]
    {
        HasVerbalComponent ? "V" : null,
        HasSomaticComponent ? "S" : null,
        HasMaterialComponent ? (string.IsNullOrEmpty(Material) ? "M" : $"M ({Material})") : null,
    }.OfType<string>());

    /// <summary>"Instantaneous", "1 minute", "8 hours", "Until dispelled".</summary>
    public string Duration => DurationType switch
    {
        "instant" => "Instantaneous",
        "timed" => DurationAmount == 1 ? $"1 {DurationUnit}" : $"{DurationAmount} {DurationUnit}s",
        "permanent" => "Until dispelled",
        _ => "Special",
    };
}
