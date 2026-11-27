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

        /// <summary>"melee" or "ranged" for a spell that makes a spell attack, else null.</summary>
        public string? AttackType { get; set; }

        /// <summary>The ability of the saving throw the spell calls for, by canonical id ("DEX"), else null.</summary>
        public string? SaveAbilityId { get; set; }

        /// <summary>What its damage is ("fire"), or the types to pick from ("acid/cold/fire"); null for none.</summary>
        public string? DamageType { get; set; }

        /// <summary>
        /// A cantrip's damage dice at each of <see cref="Services.CantripDamage.TierLevels"/>.
        /// Empty for a levelled spell, whose damage depends on the slot, and for a cantrip whose
        /// damage adds to a weapon's hit.
        /// </summary>
        public List<string> CantripDamageDice { get; set; } = [];

        public required string Description { get; set; }

        public ICollection<SpellClass> SpellClasses { get; set; } = [];
        public ICollection<SpellSubclass> SpellSubclasses { get; set; } = [];
    }
}
