using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Feats
{
    /// <summary>
    /// The ability score increase a feat gives: Points (1, and 2 only for the 2024 Ability Score
    /// Improvement feat) to one of Options, up to Maximum, which is 20, or 30 for an Epic Boon.
    /// With one option the increase is fixed: Actor raises Charisma.
    /// </summary>
    public class FeatAbilityIncrease
    {
        /// <summary>Canonical ability ids ("STR").</summary>
        public List<string> Options { get; set; } = [];

        public int Points { get; set; } = 1;

        public int Maximum { get; set; } = 20;

        /// <summary>The choice the player makes: the points over the options, all of them on one if they like.</summary>
        public AbilityIncreaseChoice ToChoice() => new(Options, Points, Points);

        /// <summary>
        /// The increase to apply for what the player picked, by canonical ability id, or null when
        /// the pick is not one the feat allows. A fixed increase needs no pick.
        /// </summary>
        public Dictionary<string, int>? Resolve(IReadOnlyDictionary<string, int> picked)
        {
            ArgumentNullException.ThrowIfNull(picked);

            if (Options.Count == 1 && picked.Values.All(amount => amount == 0))
            {
                return new Dictionary<string, int> { [Options[0]] = Points };
            }

            return ToChoice().Allows(picked)
                ? picked.Where(kvp => kvp.Value != 0).ToDictionary(kvp => AbilityScore.Normalize(kvp.Key), kvp => kvp.Value)
                : null;
        }
    }
}
