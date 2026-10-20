# Seed data generator

Writes the seed JSON in `src/CharacterCreatorApp.Infrastructure/Data` from 5etools data; how the
app reads it is in `src/CharacterCreatorApp.Infrastructure/CLAUDE.md`, and the D&D rules the
data feeds in `src/CharacterCreatorApp.Domain/CLAUDE.md`.

- Only SRD text (5.1 and 5.2, CC-BY 4.0; attribution in NOTICE.md) may be in the seed files.
  The generator's `SrdText` keeps a description only for an entry 5etools marks `srd`/`srd52`
  and lists those ids in `SrdIds.json`; every other one gets its mechanics and a book reference,
  and a non-SRD spell no material wording. `SeedDataIntegrityTests` fails on book prose outside
  that list. Never paste book text into a seed file or the generator.
- `FiveEToolsMarkup` takes 5etools markup (`{@damage 1d8}`) out of every string before the
  generator writes one, and `SourceBooks` writes `Books.json`.
- After a run, run `SeedDataIntegrityTests` (Infrastructure tests, no database).
