# Ability scores, ids and names

The rest of the D&D rules (editions, books, species, backgrounds, feats, spells, tools) are in
`dnd-rules.md`.

## Ability score keys

The same six abilities are written three different ways depending on where the data came from:

| Source | Looks like |
| --- | --- |
| A draft's `BaseAbilityScores` | `STR`, `DEX`, `CON` |
| Race and subrace seed data | `str`, `dex`, `con` |
| Ability score improvements | `strength`, `dexterity`, `constitution` |

`AbilityScore.TryNormalize` / `AbilityScore.Normalize` resolve any of those to the canonical id
(`STR`, `DEX`, `CON`, `INT`, `WIS`, `CHA`). `Character` normalizes on every write, so its
`AbilityScores` dictionary only ever holds canonical keys.

Never compare or index ability keys directly. `Character.AbilityScores` is an ordinal dictionary:
looking up `"con"` when the entry is `"CON"` silently misses and, before this was fixed, added a
second entry instead of applying the racial bonus.

## Ability increases the player picks

Some increases are fixed (a dwarf's CON +2, Actor's CHA +1) and some are the player's to place.
Every placed one is checked by one rule, `AbilityIncreaseChoice.Allows`: so many points over
these abilities, at most so many on any one.

| Where from | The choice | Kept in |
| --- | --- | --- |
| A half-elf (the race), or a variant human (the subrace, in place of the standard human's +1 to everything) | 2 points, 1 each: `Race.AbilityChoiceWith(subrace)` | `CharacterDraft.RaceAbilityIncreases` |
| A 2024 background | 3 points, at most 2 on one, over its three: `Background.AbilityChoice` | `CharacterDraft.BackgroundAbilityIncreases` |
| A feat (Athlete: Strength or Dexterity) | `Feat.AbilityIncrease`, a `FeatAbilityIncrease`; one option means it is fixed | `CharacterDraft.FeatAbilityIncreases` |

The picks arrive in any spelling and are stored under canonical ids. `FeatAbilityIncreases` has
one entry per feat, in the order of `SelectedFeatIds`, because a repeatable feat may raise a
different ability each time. `FeatAbilityIncrease.Resolve` turns a pick into the increase to
apply, and gives a fixed feat its own without one. `DraftCompletenessChecker` refuses a draft
whose picks no longer fit (reseeded data can change a feat's options), and `CharacterBuilder`
applies them after the racial bonuses and the improvements.

Every increase stops at 20, except an Epic Boon's, which goes to 30
(`FeatAbilityIncrease.Maximum`); a score already past the cap stays where it is.

A race's own feats (`Race.BonusFeatsWith`: the variant human's one of any kind, the 2024 human's
Origin one) are not improvements. They come first in `SelectedFeatIds`, and the improvements step
checks their count and category before counting the rest against `Class.ImprovementsAt(level)`. What one improvement spent on
ability points may be (2 points, at most 2 on one, real abilities) is
`AbilityIncreaseChoice.Improvement`, which the step and finalize both check.

A feat's spellcasting ability, where the player picks it (the 2024 Magic Initiate's Intelligence,
Wisdom or Charisma, `BonusSpellChoice.AbilityOptions`), is an ability id too: it arrives in any
spelling with the feat's spells and is kept under its canonical id in `FeatSpellPick.AbilityId`.

## Language ids

Languages arrive in three shapes too:

- Race languages are **display names**: `"Common"`, `"Dwarvish"`.
- Background languages contain the literal placeholder `"any"`, meaning the player chooses. `Background.LanguageChoiceCount` counts them and
  `Background.NamedLanguages` is the rest; nothing else counts the literal.
- A draft's `SelectedLanguageIds` holds **language ids of its edition**: `"common-phb"` for a 2014
  character, `"common-xphb"` for a 2024 one (`Language.SourceFor`).

`LanguageResolver` (in `Features/CharacterCreation/Building`) resolves all of them against the
edition's languages and drops the `"any"` placeholders, which the player's own choices fill.
`CharacterBuilder` uses it for the character's languages and `ChooseLanguages` for the ones the
character already has, so the two cannot disagree about what "Dwarvish" means. Language ids are
source-suffixed because the seed data carries seven different `Common` entries.

What a player may pick comes from `ILanguageRepository.GetChoosableAsync(edition)`
(`Language.ChoosableTypes`): for 2014, PHB languages without the secret ones (Druidic, Thieves'
Cant), which a class feature grants; for 2024, only the 2024 book's standard languages, the rare
ones coming from features. The language list the wizard shows and the check `ChooseLanguages`
makes both use it. A pick the race or background already gives is refused with
`CharacterDraft.LanguagesAlreadyKnown`, since the character would otherwise end up a language
short.

The 2024 rules give every new character Common and two standard languages. The 2024 species have
none, so the 2024 backgrounds carry them as `["common", "any", "any"]`, and the language step
counts them like any background's.

## Skills

Skill ids match the seeded proficiency ids (`animal_handling`, `sleight_of_hand`), so a
character's proficiency list can be matched against `Skill.All` directly, and `Skill.IsSkill` is
how to ask whether a proficiency is a skill. Don't compare the proficiency's `Type`: the data
says `"skill"`, and two places that compared with `"Skill"` left half-elves unable to choose
their skills. `SeedDataIntegrityTests` keeps `Skill.All` and the seeded skills identical.

## Proficiency ids

The separator is inconsistent in the seed data: skills and most tools use `_`
(`sleight_of_hand`, `smith_tools`), armour and a few tools use `-` (`light-armor`,
`thieves-tools`). Match on the id you were given rather than reformatting it.

What must not happen is one proficiency under two ids. The generated data once had thirteen
(`thieves_tools` next to `thieves-tools`, `longsword` next to `longswords`), because classes,
backgrounds, subraces and feats had been generated with different conventions, and a character
granted the same thing from two sources listed it twice. Each keeps the id the class data uses;
`SeedDataIntegrityTests` fails if a name gets a second id or a file names an id nothing defines.
