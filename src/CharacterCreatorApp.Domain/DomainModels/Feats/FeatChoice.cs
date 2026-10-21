using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Feats
{
    /// <summary>
    /// A pick a feat asks for beside its ability increase: skills, skills or tools, or the
    /// saving throw Resilient adds.
    /// </summary>
    public class FeatChoice
    {
        /// <summary>
        /// Resilient's: the save is the ability the feat raises (FeatAbilityIncrease), so it is
        /// not picked on its own.
        /// </summary>
        public const string SavingThrow = "saving-throw";

        /// <summary>Skill proficiencies: Keen Mind's one of five, Skill Expert's any one.</summary>
        public const string Skills = "skills";

        /// <summary>Skill or tool proficiencies, any mix: Skilled's three.</summary>
        public const string SkillsTools = "skills_tools";

        public required string Type { get; set; }
        public int Count { get; set; }
        public List<string> Options { get; set; } = [];

        /// <summary>Whether the player picks proficiencies for it (CharacterDraft.FeatProficiencyPicks).</summary>
        public bool IsProficiencyPick() => Type is Skills or SkillsTools;

        /// <summary>
        /// Whether these picks fill the choice: Count different ids, every one an option, and only
        /// skills where the choice is of skills.
        /// </summary>
        public bool Allows(IReadOnlyCollection<string> picks) =>
            IsProficiencyPick()
            && picks.Count == Count
            && picks.Distinct().Count() == Count
            && picks.All(id => Options.Contains(id) && (Type != Skills || Skill.IsSkill(id)));
    }
}
