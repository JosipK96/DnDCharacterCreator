# Domain

The domain model and the D&D rules it carries. Application handlers apply these rules too, so
read this before changing a step handler, `CharacterBuilder` or `DraftCompletenessChecker`.
How each rule is modelled and where it lives is in `docs/conventions/dnd-rules.md`, one section
per area (edits and level-ups, editions and books, races and backgrounds, ability increases,
spells and slots, languages, armour class and hit points, feats and their picks): read the
section for the area you are changing. Ability keys, language ids and proficiency ids are in
`docs/conventions/ability-scores-and-ids.md`.

## The model

- `Character` and `CharacterDraft` derive from `Entity<TId>` and expose intent-named methods
  (`ApplyAbilityScoreImprovements`, `SetRaceSkills`), never public setters on the interesting
  state.
- Derived sheet numbers (hit points, armour class, saves, skills, spell slots) come from
  `Domain/Services/CharacterStatsCalculator`. Don't recompute a modifier in a Razor page.
  `CharacterStatsInput` has no defaults, so the preview and the sheet cannot build it
  differently. A character's features aren't stored either: `CharacterFeatures` works them out
  from class, subclass, level and background, so regenerated seed data never leaves a sheet
  with dead ids.
- A completed draft stays as its character's build and is never changed; an edit or a level-up
  works on a copy (`CharacterDraft.StartEdit`, `CharacterDraft.LevelUp`), and finalize checks
  every count against the level, so a skipped choice is refused.

## Rules that caused bugs

- One rules edition per character, 2014 or 2024 (`RulesEdition`); the editions share no ids.
  `CharacterDraft.ChooseEdition` clears every choice when it changes, lookups take `?edition=`,
  and a step handler refuses an entity from the other edition with
  `CharacterDraftErrors.EditionMismatch`. A draft's books (`CharacterDraft.AllowedSources`) are
  refused the same way with `CharacterDraftErrors.SourceNotAllowed`.
- Read a class's and race's rules from the entity (`Class.SubclassLevel`, `Class.AsiLevels`,
  `Class.SpellSlotsAt`, `Class.UnarmoredArmorClassesWith`, `Race.SkillChoiceCount`), never
  hardcode one or check a class id at a call site: two separate bugs came from assuming every
  class picks a subclass at level 3. `ClassSeedDataTests` and `RaceSeedDataTests` pin the values.
- Ability keys arrive as `STR`, `str` and `strength`; always go through
  `AbilityScore.Normalize`/`TryNormalize`. `Character.AbilityScores` only ever holds canonical
  ids.
- Whether a proficiency is a skill is `Skill.IsSkill`, never a comparison with its `Type`
  string: comparing with `"Skill"` against the data's `"skill"` locked half-elves out.
- Per-level hit point bonuses (Dwarven Toughness, Draconic Resilience, Tough) are found by id in
  `HitPointBonuses`; `SeedDataIntegrityTests` fails when the data has one that list doesn't name.
- A pick the race, background, class or another pick already gives is refused, in the step
  handler and again at finalize (`DraftCompletenessChecker`); keep both in step.
