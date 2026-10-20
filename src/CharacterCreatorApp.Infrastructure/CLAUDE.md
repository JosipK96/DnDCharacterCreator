# Infrastructure

Seed data, migrations, repositories and Identity. Long form: `docs/conventions/efcore-and-postgres.md`.
The seed JSON is written by `tools/CharacterCreatorApp.SeedDataGenerator`; read its CLAUDE.md
before changing what a seed file holds.

## Repositories and Identity

- ASP.NET Core Identity stays in Infrastructure: `ApplicationUser` and `IdentityService` are in
  `Infrastructure/Identity`.
- A repository that loads a whole race, class or background writes its includes once, in a
  private `…WithDetails` query every method starts from. Per-method copies drifted, and every
  finalized character lost its race languages.

## Seed data

- Don't hand-edit a seed file the generator writes: every one but `Proficiencies.json`, which it
  reads to name what the others grant. The next run overwrites it. Change `tools/
  CharacterCreatorApp.SeedDataGenerator` and run it again.
- Reference data is seeded from JSON in `Infrastructure/Data` on startup. When the files' hash
  differs from `ReferenceDataVersions`, `SeedService` deletes every reference row and reads the
  files again in one transaction. Users stay; a draft loses only the choices the new data drops
  (`CharacterDraft.ClearMissingChoices`); a character naming one rolls the reload back, logged by
  id, and the Api starts on the old data. A character's race, class
  and background keys are deferrable `NoAction` for that, set in SQL by the `ReferenceDataSync`
  migration, so a migration that re-creates one of them must repeat it. Bump
  `ReferenceDataHash.SeederVersion` when rows change without the files changing.
- The seed files hold plain text: the generator's `FiveEToolsMarkup` takes 5etools markup
  (`{@damage 1d8}`) out of every string before it writes one, and `SeedDataIntegrityTests`
  checks none is left. Seeders read them through `SeedFile.ReadListAsync`, which refuses an
  empty file. The database holds plain text; don't render markup anywhere downstream.
- Every race, subrace, class, subclass, background, feat and spell has a `Source`, the code of
  the 5etools book it comes from (`PHB`, `XGE`, `AU`). `Books.json` lists those books with their
  edition and marks each edition's Player's Handbook; the generator's `SourceBooks` writes it
  and stops on a book it has no title for, or one with rows in both editions.
- After regenerating or editing the seed JSON, run `SeedDataIntegrityTests` (Infrastructure
  tests, no database): it catches one thing under two ids (`thieves_tools`/`thieves-tools`)
  and ids a feat or background names that nothing defines.

## EF Core and migrations

- `Character` and `CharacterDraft` ids are UUIDv7 (`Guid.CreateVersion7` from the injected
  `TimeProvider`); Identity's user ids are v7 from Npgsql. Sort by `CreatedAt`, not by id.
- Migrations: `dotnet ef migrations add <Name> --startup-project ..\CharacterCreatorApp.Api\
  CharacterCreatorApp.Api.csproj` from the Infrastructure project. **Read what it scaffolds**:
  it emitted `defaultValue: ""` for a `jsonb` column once, which Postgres rejects.
- Lists and dictionaries on `Character` are `jsonb` via `HasJsonConversion`.
- Anything implementing `IAuditable` gets `CreatedAt`/`UpdatedAt` stamped by
  `AuditableEntityInterceptor`; don't set them by hand.
