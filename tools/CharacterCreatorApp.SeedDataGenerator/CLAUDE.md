# Seed data generator

Writes the seed JSON in `src/CharacterCreatorApp.Infrastructure/Data` from 5etools data; how the
app reads it is in `src/CharacterCreatorApp.Infrastructure/CLAUDE.md`, and the D&D rules the
data feeds in `docs/conventions/dnd-rules.md`.

- Only SRD text (5.1 and 5.2, CC-BY 4.0; attribution in NOTICE.md) may be in the seed files.
  The generator's `SrdText` keeps a description only for an entry 5etools marks `srd`/`srd52`
  and lists those ids in `SrdIds.json`; every other one gets its mechanics and a book reference,
  and a non-SRD spell no material wording. `SeedDataIntegrityTests` fails on book prose outside
  that list. Never paste book text into a seed file or the generator.
- `FiveEToolsMarkup` takes 5etools markup (`{@damage 1d8}`) out of every string before the
  generator writes one, and `SourceBooks` writes `Books.json`.
- `RaceImporter` takes the races of every book but the DMG, for the edition each book was
  written for (`SourceBooks.EditionOfBook`, the one list of 2024 books, which `SpellImporter`
  reads too). A race outside the Player's Handbooks has its book in its id (`aasimar_mpmm`),
  a 5etools copy is resolved by `FiveEToolsCopy`, and what a subrace or version replaces moves
  off its race. A feat's race prerequisite is looked up in `RaceNames`, so races are imported
  before feats. The rules this follows are in `docs/conventions/dnd-rules.md`'s races section.
- A new book needs its title in `SourceBooks`, which stops the run on one it does not know.
- After a run, run `SeedDataIntegrityTests` (Infrastructure tests, no database).
