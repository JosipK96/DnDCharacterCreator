using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Domain.DomainModels.Feats
{
    public class Feat
    {
        public required string Id { get; set; }

        /// <summary>
        /// The feat's name as its book prints it. A feat in both editions has the same name in
        /// each, and the ids tell them apart ("alert_2014", "alert_2024").
        /// </summary>
        public required string Name { get; set; }

        public required string Description { get; set; }

        /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024.</summary>
        public required string Edition { get; set; }

        /// <summary>The code of the book it comes from ("PHB", "XGE"); see Books.json in the seed data.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The kind of 2024 feat: "Origin", "General", "Fighting Style", "Epic Boon" and a few
        /// that one book adds. 2014 feats have none.
        /// </summary>
        public string? Category { get; set; }

        public FeatPrerequisites Prerequisites { get; set; } = new();
        public bool IsRepeatable { get; set; }

        /// <summary>The ability score the feat raises, the player choosing which when it offers more than one; null when it raises none.</summary>
        public FeatAbilityIncrease? AbilityIncrease { get; set; }

        public List<string> GrantedProficiencyIds { get; set; } = [];
        public List<string> GrantedLanguageIds { get; set; } = [];
        public List<string> GrantedSpellIds { get; set; } = [];

        public FeatChoice? Choice { get; set; }

        /// <summary>The spells the player picks from a class list: Magic Initiate's two cantrips and a 1st-level spell.</summary>
        public BonusSpellChoice? SpellChoice { get; set; }
    }
}
