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

        public required string Type { get; set; }
        public int Count { get; set; }
        public List<string> Options { get; set; } = [];
    }
}
