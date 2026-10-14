using System.Collections.ObjectModel;

using CharacterCreatorApp.Domain.Common;
using CharacterCreatorApp.Domain.ValueObjects;

namespace CharacterCreatorApp.Domain.DomainModels.Characters
{
    public class CharacterDraft : Entity<Guid>, IAuditable
    {
        public Guid CreatedByUserId { get; private set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public string? Name { get; private set; }
        public int? Level { get; private set; }

        /// <summary>
        /// RulesEdition.Edition2014 or Edition2024: the rules the character is built with. Every
        /// class (and, as their data gains editions, race, background, feat and spell) the draft
        /// names belongs to it.
        /// </summary>
        public string Edition { get; private set; } = RulesEdition.Edition2024;

        /// <summary>
        /// The books, by code ("XPHB", "EFA"), the character may take its race, class, background,
        /// feats and spells from. Always holds the edition's Player's Handbook, and only that
        /// until the player allows more (AllowBooks).
        /// </summary>
        public List<string> AllowedSources { get; private set; } = [RulesEdition.PlayersHandbook(RulesEdition.Edition2024)];

        public string? RaceId { get; private set; }
        public string? SubraceId { get; private set; }

        /// <summary>
        /// The size picked for a species that may be either (Race.SizeOptions: a 2024 human's
        /// "Small or Medium"). Null for a race with one size.
        /// </summary>
        public string? Size { get; private set; }

        /// <summary>The spells picked for the race or subrace (Race.SpellChoiceWith): a 2014 high elf's wizard cantrip.</summary>
        public List<string> RaceSpellIds { get; private set; } = [];

        /// <summary>
        /// The +1s picked for the race or subrace (Race.AbilityChoiceWith), by canonical id: a
        /// half-elf's two, a variant human's two. Empty for a race without that choice.
        /// </summary>
        public Dictionary<string, int> RaceAbilityIncreases { get; private set; } = [];

        public string? ClassId { get; private set; }
        public string? SubclassId { get; private set; }

        public string? BackgroundId { get; private set; }

        /// <summary>
        /// A 2024 background's ability increases, +2 and +1 or +1 to three abilities, by canonical
        /// id ("STR"). Empty for a 2014 background, whose race gives the increases instead.
        /// </summary>
        public Dictionary<string, int> BackgroundAbilityIncreases { get; private set; } = [];

        /// <summary>
        /// The feat picked for a background that lets the player choose it (Background.FeatChoiceCategory:
        /// a Mist Wanderer's Dark Gift). Null for a background whose feat is fixed.
        /// </summary>
        public string? BackgroundFeatId { get; private set; }

        public Dictionary<string, int> BaseAbilityScores { get; private set; } = [];

        /// <summary>
        /// How the base scores were generated (AbilityScoreMethod), so they can be checked against
        /// what that method allows. Null for a draft saved before the method was recorded.
        /// </summary>
        public string? BaseAbilityScoreMethod { get; private set; }
        public List<string> SelectedClassProficiencyIds { get; private set; } = [];
        public List<string> SelectedRaceSkillIds { get; private set; } = [];
        public List<string> SelectedLanguageIds { get; private set; } = [];
        public List<string> SelectedFeatIds { get; private set; } = [];

        /// <summary>
        /// What each of SelectedFeatIds raises, in the same order, by canonical id: {"DEX": 1} for
        /// an Athlete who picked Dexterity, empty for a feat that raises nothing. A repeatable feat
        /// can raise a different ability each time, so this follows the list rather than the ids.
        /// </summary>
        public List<Dictionary<string, int>> FeatAbilityIncreases { get; private set; } = [];

        /// <summary>
        /// The Fighting Style feats picked for the class's Fighting Style features
        /// (CharacterFeatures.FightingStylesOwed), apart from the feats taken for improvements.
        /// </summary>
        public List<string> FightingStyleIds { get; private set; } = [];

        public List<string> SelectedSpellIds { get; private set; } = [];

        /// <summary>
        /// The spells picked for each feat with a spell choice (Magic Initiate), one per such feat
        /// in the character's order: the background's, the Fighting Styles, then SelectedFeatIds.
        /// Changing any of those feats clears it.
        /// </summary>
        public List<FeatSpellPick> FeatSpells { get; private set; } = [];

        // Each element is one ASI use: {"STR": 2} or {"STR": 1, "DEX": 1}
        public List<Dictionary<string, int>> AbilityScoreImprovements { get; private set; } = [];

        public bool IsCompleted { get; private set; }

        public static CharacterDraft CreateEmpty(Guid userId, TimeProvider timeProvider)
        {
            var now = timeProvider.GetUtcNow();

            return new CharacterDraft
            {
                // Version 7 starts with the creation time, so new rows go to the end of the
                // primary key index instead of a random page; user ids are already version 7
                Id = Guid.CreateVersion7(now),
                CreatedByUserId = userId,
                CreatedAt = now,
                IsCompleted = false
            };
        }

        /// <summary>
        /// Switches the rules the character is built with. The two editions share no ids, so a
        /// switch clears every choice that named one: race, class, background, and the skills,
        /// languages, feats, spells and improvements picked for them. The name, level and ability
        /// scores stay. The books go back to the new edition's Player's Handbook: no book is in both.
        /// </summary>
        public void ChooseEdition(string edition)
        {
            if (!RulesEdition.IsKnown(edition))
            {
                throw new ArgumentException($"Unknown rules edition: {edition}", nameof(edition));
            }

            if (Edition == edition)
            {
                return;
            }

            Edition = edition;
            AllowedSources = [RulesEdition.PlayersHandbook(edition)];
            RaceId = null;
            SubraceId = null;
            Size = null;
            RaceSpellIds = [];
            RaceAbilityIncreases = [];
            ClassId = null;
            SubclassId = null;
            BackgroundId = null;
            BackgroundFeatId = null;
            BackgroundAbilityIncreases = [];
            SelectedClassProficiencyIds = [];
            SelectedRaceSkillIds = [];
            SelectedLanguageIds = [];
            SelectedFeatIds = [];
            FeatAbilityIncreases = [];
            FightingStyleIds = [];
            FeatSpells = [];
            SelectedSpellIds = [];
            AbilityScoreImprovements = [];
        }

        public bool AllowsSource(string source) => AllowedSources.Contains(source);

        /// <summary>
        /// Allows these books and the edition's Player's Handbook, and clears every choice from a
        /// book no longer allowed, the way changing that choice would: a race takes its skills,
        /// languages and improvements with it, a feat taken for an improvement the whole
        /// improvements step. A Fighting Style or spell goes alone, and its step asks for another.
        /// The caller checks the books belong to the draft's edition.
        /// </summary>
        public void AllowBooks(IEnumerable<string> books, DraftChoiceSources choiceSources)
        {
            ArgumentNullException.ThrowIfNull(books);
            ArgumentNullException.ThrowIfNull(choiceSources);

            AllowedSources = books.Prepend(RulesEdition.PlayersHandbook(Edition)).Distinct().ToList();

            if (IsRemoved(choiceSources.Race))
            {
                RaceId = null;
                SubraceId = null;
                Size = null;
                RaceSpellIds = [];
                RaceAbilityIncreases = [];
                SelectedRaceSkillIds = [];
                SelectedLanguageIds = [];
                ClearImprovements();
            }
            else if (IsRemoved(choiceSources.Subrace))
            {
                SetSubrace(null);
            }

            if (IsRemoved(choiceSources.Class))
            {
                ClassId = null;
                SubclassId = null;
                SelectedClassProficiencyIds = [];
                SelectedSpellIds = [];
                ClearFightingStyles();
                ClearImprovements();
            }
            else if (IsRemoved(choiceSources.Subclass))
            {
                SetSubclass(null);
            }

            if (IsRemoved(choiceSources.Background))
            {
                BackgroundId = null;
                BackgroundFeatId = null;
                BackgroundAbilityIncreases = [];
                SelectedLanguageIds = [];
                FeatSpells = [];
            }

            if (BackgroundFeatId != null && IsRemoved(choiceSources.Feats.GetValueOrDefault(BackgroundFeatId)))
            {
                BackgroundFeatId = null;
                FeatSpells = [];
            }

            if (SelectedFeatIds.Exists(id => IsRemoved(choiceSources.Feats.GetValueOrDefault(id))))
            {
                ClearImprovements();
            }

            if (FightingStyleIds.Exists(id => IsRemoved(choiceSources.Feats.GetValueOrDefault(id))))
            {
                FightingStyleIds = FightingStyleIds.FindAll(id => !IsRemoved(choiceSources.Feats.GetValueOrDefault(id)));
                FeatSpells = [];
            }

            SelectedSpellIds = SelectedSpellIds.FindAll(id => !IsRemoved(choiceSources.Spells.GetValueOrDefault(id)));
            RaceSpellIds = RaceSpellIds.FindAll(id => !IsRemoved(choiceSources.Spells.GetValueOrDefault(id)));

            // One pick per feat, in order, so a pick losing a spell takes the others with it
            if (FeatSpells.Exists(pick => pick.SpellIds.Exists(id => IsRemoved(choiceSources.Spells.GetValueOrDefault(id)))))
            {
                FeatSpells = [];
            }
        }

        // A choice whose book is unknown (gone after a reseed) is left for finalize to report
        private bool IsRemoved(string? source) => source != null && !AllowsSource(source);

        public void SetNameAndLevel(string name, int level)
        {
            if (Level != level)
            {
                // How many improvements, spells and Fighting Styles the character may take depends
                // on level, and a feat's prerequisites may name one
                ClearImprovements();
                SelectedSpellIds = [];
                ClearFightingStyles();
            }

            Name = name;
            Level = level;
        }

        /// <summary>
        /// Sets the class and optionally the subclass.
        /// Use this when setting both at once, or use SetSubclass() separately.
        /// </summary>
        public void SetClass(string classId, string? subclassId = null)
        {
            if (ClassId != classId)
            {
                // Proficiency options, spell lists, improvement levels and the class features feats
                // ask for are all class-specific
                SelectedClassProficiencyIds = [];
                SelectedSpellIds = [];
                ClearFightingStyles();
                ClearImprovements();
            }

            ClassId = classId;
            SetSubclass(subclassId);
        }

        /// <summary>
        /// Sets or updates the subclass without changing the class.
        /// Useful for:
        /// - Changing subclass selection
        /// - Adding subclass after class is already set
        /// - Level-up requiring subclass selection
        /// Another subclass clears the spells: it changes the lists they come from (an Eldritch
        /// Knight's wizard spells) and the spell table itself. It clears the Fighting Styles too,
        /// a Champion having one more than another fighter.
        /// </summary>
        public void SetSubclass(string? subclassId)
        {
            if (SubclassId != subclassId)
            {
                SelectedSpellIds = [];
                ClearFightingStyles();
            }

            SubclassId = subclassId;
        }

        /// <summary>
        /// Sets the race and subrace, the +1s picked for them (Race.AbilityChoiceWith), stored
        /// under canonical ability ids, and the size picked for a race that may be either. Use
        /// SetSubrace to change only the subrace. Another race or subrace clears the improvements
        /// step, whose feats start with the race's own, and the spells picked for the old one.
        /// </summary>
        public void SetRace(
            string raceId, string? subraceId = null, IReadOnlyDictionary<string, int>? abilityIncreases = null, string? size = null)
        {
            if (RaceId != raceId)
            {
                // Skill and language allowances are race-specific
                SelectedRaceSkillIds = [];
                SelectedLanguageIds = [];
            }

            if (RaceId != raceId || SubraceId != subraceId)
            {
                // The race's bonus feats, which come first among the feats, change with the race
                // and with the subrace (a variant human's feat slot)
                ClearImprovements();
                RaceSpellIds = [];
            }

            RaceId = raceId;
            SubraceId = subraceId;
            RaceAbilityIncreases = Canonical(abilityIncreases);
            Size = size;
        }

        /// <summary>
        /// Sets or updates the subrace without changing the race. Another subrace may bring its
        /// own ability choice (a variant human's), so the +1s picked for the old one go.
        /// </summary>
        public void SetSubrace(string? subraceId)
        {
            if (SubraceId != subraceId)
            {
                RaceAbilityIncreases = [];
                RaceSpellIds = [];
                ClearImprovements();
            }

            SubraceId = subraceId;
        }

        /// <summary>
        /// Sets the background, for a 2024 one the ability increases picked from it (see
        /// Background.AllowsAbilityIncreases), stored under canonical ability ids, and the feat
        /// picked for one that lets the player choose it (Background.FeatChoiceCategory).
        /// </summary>
        public void SetBackground(string backgroundId, IReadOnlyDictionary<string, int>? abilityIncreases = null, string? featId = null)
        {
            if (BackgroundId != backgroundId)
            {
                // Backgrounds grant differing numbers of free languages
                SelectedLanguageIds = [];
            }

            if (BackgroundId != backgroundId || BackgroundFeatId != featId)
            {
                // The background's feat comes first among the feats whose spells are picked
                FeatSpells = [];
            }

            BackgroundId = backgroundId;
            BackgroundAbilityIncreases = Canonical(abilityIncreases);
            BackgroundFeatId = featId;
        }

        public void SetBaseAbilityScores(Dictionary<string, int> scores, string method)
        {
            BaseAbilityScores = new Dictionary<string, int>(scores);
            BaseAbilityScoreMethod = method;
        }

        public void SetClassProficiencies(List<string> proficiencyIds)
        {
            SelectedClassProficiencyIds = proficiencyIds;
        }

        public void SetRaceSkills(List<string> proficiencyIds)
        {
            SelectedRaceSkillIds = proficiencyIds;
        }

        public void SetLanguages(List<string> languageIds)
        {
            SelectedLanguageIds = languageIds;
        }

        /// <summary>
        /// Sets the feats taken, and what each raises (FeatAbilityIncrease.Resolve): one entry
        /// per feat, in order, or none at all for feats that raise nothing.
        /// </summary>
        public void SetFeats(List<string> featIds, List<Dictionary<string, int>>? abilityIncreases = null)
        {
            ArgumentNullException.ThrowIfNull(featIds);

            if (abilityIncreases != null && abilityIncreases.Count != featIds.Count)
            {
                throw new ArgumentException(
                    $"{featIds.Count} feat(s) but {abilityIncreases.Count} ability increase(s); give one per feat.", nameof(abilityIncreases));
            }

            if (!featIds.SequenceEqual(SelectedFeatIds))
            {
                FeatSpells = [];
            }

            FeatAbilityIncreases = abilityIncreases?.ConvertAll(increase => Canonical(increase)) ?? featIds.ConvertAll(_ => new Dictionary<string, int>());
            SelectedFeatIds = featIds;
        }

        /// <summary>
        /// What the player picked for the feat at this place in SelectedFeatIds to raise; empty
        /// where nothing was picked.
        /// </summary>
        public IReadOnlyDictionary<string, int> FeatAbilityIncreaseAt(int index) =>
            index < FeatAbilityIncreases.Count ? FeatAbilityIncreases[index] : ReadOnlyDictionary<string, int>.Empty;

        public void SetAbilityScoreImprovements(List<Dictionary<string, int>> improvements)
        {
            AbilityScoreImprovements = improvements;
        }

        public void ChooseFightingStyles(List<string> featIds)
        {
            ArgumentNullException.ThrowIfNull(featIds);

            // A Fighting Style may have spells to pick: a 2024 Blessed Warrior's two cantrips
            if (!featIds.SequenceEqual(FightingStyleIds))
            {
                FeatSpells = [];
            }

            FightingStyleIds = featIds;
        }

        public void SetSpells(List<string> spellIds)
        {
            SelectedSpellIds = spellIds;
        }

        /// <summary>
        /// Sets the spells picked for the race (Race.SpellChoiceWith) and for each feat with a
        /// spell choice, one pick per feat in the character's order (see FeatSpells).
        /// </summary>
        public void ChooseBonusSpells(List<string> raceSpellIds, List<FeatSpellPick> featSpells)
        {
            ArgumentNullException.ThrowIfNull(raceSpellIds);
            ArgumentNullException.ThrowIfNull(featSpells);

            RaceSpellIds = raceSpellIds;
            FeatSpells = featSpells;
        }

        // The improvements step as a whole: the points, the feats taken instead and what they
        // raise. The count of improvements includes the feats, so they only go together.
        private void ClearImprovements()
        {
            AbilityScoreImprovements = [];
            SelectedFeatIds = [];
            FeatAbilityIncreases = [];
            FeatSpells = [];
        }

        // The spells picked for the feats follow the feats, Fighting Styles among them
        private void ClearFightingStyles()
        {
            FightingStyleIds = [];
            FeatSpells = [];
        }

        public void MarkAsCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Checks the invariants the draft can verify on its own. Choices whose required
        /// count depends on the chosen race, class or background (proficiencies, race skills,
        /// languages, ability score improvements) are checked when the character is finalized,
        /// because only then are those entities loaded.
        /// </summary>
        public bool IsReadyToFinalize()
        {
            return !string.IsNullOrWhiteSpace(Name)
                && Level is >= 1 and <= 20
                && !string.IsNullOrWhiteSpace(RaceId)
                && !string.IsNullOrWhiteSpace(ClassId)
                && !string.IsNullOrWhiteSpace(BackgroundId)
                && HasAllAbilityScores();
        }

        /// <summary>
        /// True when all six abilities have a score. Counting entries is not enough: the keys
        /// arrive in several spellings, so six entries could still name the same ability twice.
        /// </summary>
        private bool HasAllAbilityScores()
        {
            var abilities = new HashSet<string>();

            foreach (var key in BaseAbilityScores.Keys)
            {
                if (!AbilityScore.TryNormalize(key, out var id))
                {
                    return false;
                }

                abilities.Add(id);
            }

            return abilities.Count == AbilityScore.All.Count;
        }

        // Increases under canonical ids, the zeros left out; the choice that allowed them has
        // already checked every key is an ability
        private static Dictionary<string, int> Canonical(IReadOnlyDictionary<string, int>? increases) =>
            (increases ?? new Dictionary<string, int>())
                .Where(kvp => kvp.Value != 0)
                .ToDictionary(kvp => AbilityScore.Normalize(kvp.Key), kvp => kvp.Value);
    }
}
