# Domain

The domain model and the D&D rules it carries. Application handlers apply these rules too, so
read this before changing a step handler, `CharacterBuilder` or `DraftCompletenessChecker`.
Ability keys, language ids and proficiency ids are explained at more length in
`docs/conventions/ability-scores-and-ids.md`.

## The model

- `Character` and `CharacterDraft` derive from `Entity<TId>` and expose intent-named methods
  (`ApplyAbilityScoreImprovements`, `SetRaceSkills`), never public setters on the interesting
  state.
- Derived sheet numbers (hit points, armour class, saves, skills) come from
  `Domain/Services/CharacterStatsCalculator`. Don't recompute a modifier in a Razor page. A
  character's features aren't stored either: `CharacterFeatures` works them out from class,
  subclass, level and background, so regenerated seed data never leaves a sheet with dead ids.

## D&D rules

- A character is built with one rules edition, 2014 or 2024 (`RulesEdition`), picked in step 1:
  `CharacterDraft.ChooseEdition` clears every choice when it changes, because the editions share
  no ids (the 2024 wizard is `wizard-2024`, the 2014 one `wizard`). Lookups take `?edition=`,
  and a step handler refuses an entity from the other edition with
  `CharacterDraftErrors.EditionMismatch`. Classes, races, backgrounds, spells, feats and the
  languages a player picks are all split by edition (a reprint is a second spell,
  `fireball_2024`).
- A draft also keeps the books it may use (`CharacterDraft.AllowedSources`, book codes as in
  `Books.json`), picked in step 1. It always holds the edition's Player's Handbook
  (`RulesEdition.PlayersHandbook`) and only that until the player ticks more, and `ChooseEdition`
  resets it. The wizard's lookups take the books as `?sources=` (none means every book), a step
  handler refuses an entity from another book with `CharacterDraftErrors.SourceNotAllowed`, and
  `DraftCompletenessChecker` does the same at finalize. `CharacterDraft.AllowBooks` clears the
  choices from a book taken away, through the same cascades as changing them.
- A 2024 species is a race (`elf_2024`), and its lineages, ancestries and legacies are its
  subraces (`elf_drow_lineage_2024`, `dragonborn_black_2024`). It has no ability increases or
  languages; those come with the 2024 background. A race and its subrace give spells from a
  character level (`Race.SpellIdsAt`, added by `CharacterBuilder`), and a subrace may change
  the speed (`Race.SpeedWith`) and add language picks (`Race.LanguageChoiceCountWith`).
- A 2024 background raises three abilities, +2 and +1 or +1 to each, picked in the background
  step (`Background.AllowsAbilityIncreases`, kept in `CharacterDraft.BackgroundAbilityIncreases`),
  and gives an origin feat (`Background.FeatId`, which some 2014 ones have too); it has no
  feature. `CharacterBuilder` applies both, and a feat taken for an improvement may not repeat
  it. The 2024 rules' Common and two standard languages for every character are carried on the
  2024 backgrounds (`["common", "any", "any"]`), so the language step needs nothing else.
- Every picked increase is an `AbilityIncreaseChoice` (points over options, at most so many per
  ability): a 2024 background's, and a race's or subrace's +1s (`Race.AbilityChoiceWith`: a
  half-elf's two, a variant human's two of six), kept in `CharacterDraft.RaceAbilityIncreases`.
  The 2014 human is a standard or a variant one, subraces the generator makes from 5etools'
  nameless one beside the named one, and a race with subraces needs one at finalize. A race's
  own feats (`Race.BonusFeatsWith`: the variant human's any one, the 2024 human's Origin one)
  come first in `SelectedFeatIds`. What a feat raises is `Feat.AbilityIncrease` (up to 20, 30
  for an Epic Boon); the pick is in `CharacterDraft.FeatAbilityIncreases`, one per feat in the
  same order, and `FeatAbilityIncrease.Resolve` turns it into the increase (a fixed one needs none).
- A subclass gives spells two ways (`SpellSubclass.IsAlwaysPrepared`): domain and oath spells
  come with it at their level (`Subclass.AlwaysPreparedSpellIdsAt`, added by `CharacterBuilder`),
  and a 2014 patron's only join the list the warlock picks from. `Subclass.SpellListClassId`
  opens a whole other list: the Wizard's for an Eldritch Knight, whose fighter has none. The
  spells step and `ChooseSpells` both take all three into account.
- A class's rules (`Class.SubclassLevel`, `Class.AsiLevels`, `Class.SavingThrowAbilityIds`,
  `Class.UnarmoredArmorClass`, the spell table) and a race's (`Race.SkillChoiceCount` and its options, `Race.
  LanguageChoiceCount`) are read from the 5etools data by the generator. Read them from the
  entity and never hardcode one at a call site — two separate bugs came from assuming every
  class picks a subclass at level 3. `ClassSeedDataTests` and `RaceSeedDataTests` pin the
  values to the book, a row per class and race.
- Ability keys arrive as `STR`, `str` and `strength` depending on the source; always go through
  `AbilityScore.Normalize`/`TryNormalize`. `Character.AbilityScores` only ever holds canonical
  ids.
- Base scores come with the method that made them (`AbilityScoreMethod`: the standard array,
  27-point buy with 8 to 15, or 3 to 18 rolled), and the server checks they fit it, in the
  validator and again in `DraftCompletenessChecker`. A draft from before the method was kept
  has to pick its scores again.
- Language ids are source-suffixed (`common-phb`, `common-xphb`): a 2014 character's are the
  PHB's, a 2024 one's the 2024 book's (`Language.SourceFor`). Race languages are stored as
  display names and background languages contain `"any"` placeholders; `LanguageResolver`
  resolves all three against the edition's languages, for `CharacterBuilder` and
  `ChooseLanguages` alike. What a player may pick is `ILanguageRepository.GetChoosableAsync`
  (`Language.ChoosableTypes`: any PHB language but the secret ones in 2014, the standard ones
  in 2024), and a pick the race or background already gives is refused.
- Armour is not modelled, so armour class is the unarmoured one and the sheet says so: 10 + DEX,
  or the class's or subclass's own when higher (`Class.UnarmoredArmorClass`, `Subclass.
  UnarmoredArmorClass`): a barbarian's 10 + DEX + CON and a monk's 10 + DEX + WIS (both
  editions), a draconic sorcerer's 13 + DEX (2014) or 10 + DEX + CHA (2024), and a 2024 Dance
  bard's or Noble Genies paladin's 10 + DEX + CHA. The generator reads them from the feature text, with whether a shield still
  allows it (a monk's and a Dance bard's do not), for when shields are modelled. Both handlers
  pass `Class.UnarmoredArmorClassesWith` through `CharacterStatsInput`; never check a class id.
- Hit points some choices add every level (Dwarven Toughness, a 2014 hill dwarf's and every
  2024 dwarf's; Draconic Resilience; Tough) are in `Domain/Services/HitPointBonuses`.
  `CharacterStatsInput` has no defaults, so the preview and the sheet cannot build it
  differently.
- Whether a proficiency is a skill is `Skill.IsSkill`, never a comparison with its `Type`
  string: comparing with `"Skill"` against the data's `"skill"` locked half-elves out.
- A feat belongs to one edition (`Feat.Edition`; 2024 ones have a `Category`, "Origin" or
  "Epic Boon"). One in both keeps its book's name in each, and its id says which:
  `alert_2014`, `alert_2024`. Who may take one is `Feat.Prerequisites.FindUnmet`: level,
  ability minimums (racial bonuses count, and so does every improvement for a feat taken for
  one, but not for a race's own feat), race, class, and class features by name ("Fighting
  Style", "Spellcasting" or "Pact Magic", from `CharacterFeatures.ClassFeaturesFor`). What it
  cannot check (another feat, a campaign) is shown, not enforced. Classes, subclasses, their
  features, races, backgrounds, spells and feats are generated from 5etools; Tasha's optional
  class features, and the choices the app does not ask for yet (a 2014 House Agent's set of
  tools, Skill Expert's expertise), are left out.
- The skills or tools a feat lets the player pick are its `Choice` (`FeatChoice.IsProficiencyPick`:
  `skills`, Keen Mind's one of five, or `skills_tools`, Skilled's three; `FeatChoice.Allows`
  checks a pick, `Skill.IsSkill` for a skills one). They are picked in the improvements step,
  the background's feat (a Noble's Skilled) among them, and kept in
  `CharacterDraft.FeatProficiencyPicks`, one per such feat: the background's feat, then `SelectedFeatIds`, so
  Skilled taken twice has two. Another background feat or set of feats clears them, and a pick
  the race, background, class or another pick gives is refused as other proficiency picks are.
- Tools, instruments and gaming sets a race, background or class lets the player pick (a 2014
  dwarf's artisan's tools, a Criminal's gaming set, a bard's three instruments) are
  `ToolChoice`s on `Race`, `Background` and `Class`; a kind ("any gaming set") is every
  proficiency of that type in `Proficiencies.json` (`artisans_tools`, `musical-instrument`,
  `gaming_set`). They are picked in the proficiencies step and kept in `CharacterDraft.ToolPicks`,
  one list per choice in race, background, class order (`ToolPicks.SlotsFor`), and checked by
  `ChooseTools` and finalize alike.
- Spells a race or feat lets the player pick from a class list (a 2014 high elf's wizard
  cantrip, Magic Initiate's) are `BonusSpellChoice`s on `Race`, `Subrace` and `Feat`, picked in
  the spells step and kept in `CharacterDraft.RaceSpellIds` and `FeatSpells`, one pick per such
  feat in the character's feat order. `Character.FeatSpells` keeps the feat picks after
  finalize, so the sheet, like the review step, lists a feat taken twice twice, each with its
  own spells (`CharacterFeatEntries` pairs them for both). A feat whose data lets the player pick
  the spellcasting ability (`BonusSpellChoice.AbilityOptions`: the 2024 Magic Initiate's INT, WIS
  or CHA) keeps the pick with its spells (`FeatSpellPick.AbilityId`), and the sheet shows it with
  them. `BonusSpells` lists what a draft owes and checks the
  picks, for `ChooseBonusSpells` and finalize alike; a background may fix its feat's list
  (`Background.FeatSpellListClassId`: an Acolyte's is the cleric's). A 2024 species that is
  "Small or Medium" keeps its pick in `CharacterDraft.Size`, a background that lets the player
  pick its feat (`Background.FeatChoiceCategory`) in `BackgroundFeatId`, and Resilient's saving
  throw is the ability it raised (`FeatChoice.SavingThrow`).
