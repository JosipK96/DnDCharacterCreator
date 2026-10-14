using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Spells;

namespace CharacterCreatorApp.Application.ReadModels
{
    public class FeatReadModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }

        /// <summary>RulesEdition.Edition2014 or RulesEdition.Edition2024.</summary>
        public required string Edition { get; set; }

        /// <summary>The code of the book it comes from ("PHB", "XGE").</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>"Origin", "General", "Fighting Style", "Epic Boon"; null for a 2014 feat.</summary>
        public string? Category { get; set; }

        public FeatPrerequisites Prerequisites { get; set; } = new();
        public bool IsRepeatable { get; set; }

        /// <summary>The ability score the feat raises, the player picking which of several; null when it raises none.</summary>
        public FeatAbilityIncrease? AbilityIncrease { get; set; }

        public IReadOnlyCollection<string> GrantedProficiencyIds { get; set; } = [];
        public IReadOnlyCollection<string> GrantedLanguageIds { get; set; } = [];
        public IReadOnlyCollection<string> GrantedSpellIds { get; set; } = [];

        public FeatChoiceReadModel? Choice { get; set; }

        /// <summary>The spells the player picks from a class list: Magic Initiate's.</summary>
        public BonusSpellChoice? SpellChoice { get; set; }
    }
}
