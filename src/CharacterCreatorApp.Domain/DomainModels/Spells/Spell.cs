namespace CharacterCreatorApp.Domain.DomainModels.Spells
{
    public class Spell
    {
        public required string Id { get; set; }
        public required string Name { get; set; }

        /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
        public string Source { get; set; } = string.Empty;
        public int Level { get; set; }
        public required string School { get; set; }
        public int CastingTimeNumber { get; set; }
        public required string CastingTimeUnit { get; set; }  // "action", "bonus_action", "minute"
        public required string RangeType { get; set; }  // "self", "touch", "point"
        public string? RangeDistanceType { get; set; }  // "feet", "miles", "touch"
        public int? RangeDistanceAmount { get; set; }
        public bool HasVerbalComponent { get; set; }
        public bool HasSomaticComponent { get; set; }
        public bool HasMaterialComponent { get; set; }
        public string? Material { get; set; }
        public required string DurationType { get; set; }  // "instant", "timed", "until_dispelled"
        public int? DurationAmount { get; set; }
        public string? DurationUnit { get; set; }  // "minute", "hour", "round"
        public bool IsConcentration { get; set; }
        public bool IsRitual { get; set; }
        public required string Description { get; set; }

        public ICollection<SpellClass> SpellClasses { get; set; } = [];
        public ICollection<SpellSubclass> SpellSubclasses { get; set; } = [];
    }
}
