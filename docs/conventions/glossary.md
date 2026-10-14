# Glossary

D&D 5th Edition terms as this codebase uses them, and where each one lives.

## Character creation

| Term | Meaning | In the code |
|---|---|---|
| **Draft** | A character being built. Survives across sessions; becomes a `Character` when finalized | `CharacterDraft` |
| **Finalize** | Turning a completed draft into a character. One-way | `FinalizeCharacterCommandHandler` |
| **Preview** | The character a draft *would* produce, worked out without saving | `PreviewCharacterQueryHandler` |

## The rulebook

| Term | Meaning | In the code |
|---|---|---|
| **Edition** | The 2014 rules or their 2024 revision. A character follows one, picked in the first step; the two share no ids | `RulesEdition`, `CharacterDraft.Edition` |
| **PHB** | Player's Handbook: the 2014 one (`PHB`) and the 2024 one (`XPHB` in the data). A character's languages come from its edition's | `Language.SourceFor` |
| **Race** | Dwarf, elf, human… A 2014 race grants ability bonuses and languages; the 2024 ones, called species, grant neither. Sometimes proficiencies and spells | `Race` |
| **Subrace** | A variant of a race — hill dwarf, high elf, standard or variant human; for a 2024 species its lineage, ancestry or legacy (drow lineage, black dragonborn). A race that has them needs one | `Subrace` |
| **Class** | Fighter, wizard, cleric… Determines hit die, saving throws and progression | `Class` |
| **Subclass** | A specialisation within a class — Champion, School of Evocation, Life Domain. Chosen at a level that **differs by class** | `Subclass`, `Class.SubclassLevel` |
| **Background** | What the character did before adventuring. Grants proficiencies and languages, and a feature (2014) or three ability increases and an origin feat (2024) | `Background` |

## Numbers

| Term | Meaning | In the code |
|---|---|---|
| **Ability score** | One of the six: Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma. 1–20 in practice | `AbilityScore`, keys `STR`…`CHA` |
| **Modifier** | `(score - 10) / 2`, rounded **down**, including below 10 | `CharacterStatsCalculator.GetAbilityModifier` |
| **Proficiency bonus** | +2 at levels 1–4, rising every four levels to +6 | `CharacterStatsCalculator.GetProficiencyBonus` |
| **Hit die** | The die a class rolls for hit points per level — `1d10` for a fighter | `Class.HitDie` |
| **Hit points** | Max die at level 1, then the die's average rounded up per level, plus the CON modifier each level | `CharacterStatsCalculator` |
| **AC** | Armour class. Unarmoured it is `10 + DEX`. Armour is not modelled | `CharacterStats.ArmorClass` |
| **Saving throw** | A roll to resist something. A class is proficient in exactly two | `Class.SavingThrowAbilityIds` |
| **Passive Perception** | `10 + the Perception skill modifier` | `CharacterStats.PassivePerception` |

## Choices

| Term | Meaning | In the code |
|---|---|---|
| **Proficiency** | Something the character is trained in: a skill, a weapon, armour, a tool. Adds the proficiency bonus where it applies | `Proficiency`, `Type` of `skill`, `armor`, `weapon`, `tool`… |
| **Skill** | One of the eighteen: Athletics, Stealth, Perception… Each checked against a fixed ability | `Skill` |
| **ASI** | Ability Score Improvement. Earned at class-specific levels; **either** +2 ability points **or** a feat | `Class.AsiLevels`, `CharacterDraft.AbilityScoreImprovements` |
| **Feat** | A special ability taken in place of an ASI, given by a background (a 2024 origin feat), or picked for a race (a variant human's, a 2024 human's Origin feat). May have prerequisites, and may raise an ability | `Feat`, `CharacterDraft.SelectedFeatIds`, `Background.FeatId`, `Race.BonusFeatsWith` |
| **Ability increase** | +1 or +2 to an ability from a race, a 2024 background, a feat or an ASI. Some are fixed (a dwarf's CON +2), some the player places (a half-elf's two +1s) | `AbilityIncreaseChoice`, `FeatAbilityIncrease` |
| **Cantrip** | A spell of level 0, castable at will | Spell `Level == 0` |
| **Spell slot** | A per-day resource for casting. **Not modelled yet** | — |

## Terms that mean something specific here

**"any"** — in background language data this is a literal placeholder meaning
"the player picks one", not a language. `CharacterBuilder` drops it and the
player's own choice fills the slot.

**Race language vs language id** — race languages are stored as display names
(`"Common"`); drafts store the ids of their edition's languages (`"common-phb"`,
`"common-xphb"`). See `ability-scores-and-ids.md`.

**Feature vs feat** — a *feature* comes automatically from a class, subclass or
2014 background (`Feature`, worked out by `CharacterFeatures`). A *feat* is
chosen in place of an ASI or given by a background (`Feat`, `Character.FeatIds`).
Easy to conflate; they are different tables.
