# EF Core and PostgreSQL

## Version

Production's database (Neon) runs PostgreSQL 18, and the compose database and every Testcontainers
fixture run `postgres:18-alpine` to match, so a migration or query that fails only on 18 fails in
CI. A Neon major-version upgrade means changing all of them, and `postgres:18` in the backup
workflow and restore docs, in one pull request.

## Migrations

Run from the Infrastructure project, which holds both the `DbContext` and the
migrations, with the API as the startup project so configuration resolves:

```bash
dotnet ef migrations add <Name> --startup-project ..\CharacterCreatorApp.Api\CharacterCreatorApp.Api.csproj
```

The API applies pending migrations on startup outside the `Testing` environment,
so running it is how a migration reaches your local database.

### Read what it scaffolds

EF's generated migration is a draft, not an answer. It has been wrong in this
repo: adding `FeatIds` produced

```csharp
defaultValue: ""        // PostgreSQL rejects this as invalid jsonb
```

which had to become `defaultValueSql: "'[]'::jsonb"`. Check every generated
migration before committing it, particularly:

- defaults on `jsonb` columns
- a new non-nullable column on a table with rows
- anything that drops or renames rather than adding

## jsonb columns

`Character`'s collections and dictionary are stored as `jsonb` through
`HasJsonConversion()`:

```csharp
builder.Property(c => c.AbilityScores)
    .HasColumnType("jsonb")
    .HasJsonConversion();
```

There are two overloads in `ModelBuilderExtensions`, and both set a
`ValueComparer`, so every such column is change-tracked by content: the
`Dictionary<string, int>` one compares entries, and the generic one compares the JSON.
It serves `Character`'s lists (`ProficiencyIds`, `LanguageIds`, `SpellIds`, `FeatIds`,
`FeatSavingThrowAbilityIds`), its `FeatSpells` and `HitDieResults`, and the draft's
choices (`AbilityScoreImprovements`, `RaceAbilityIncreases`, `BackgroundAbilityIncreases`,
`FeatAbilityIncreases`, `FeatProficiencyPicks`, `FeatSpells`, `ToolPicks`,
`HitDieResults`); the full list is in `CharacterConfiguration` and
`CharacterDraftConfiguration`. A character's features are not stored: `CharacterFeatures`
works them out.
The generic one used to have none; EF then compared the list reference, so an
`Add` to a loaded list was never saved, and it warned about each column at
startup.

`CharacterDraft`'s other lists (`SelectedSpellIds` and the like) are not jsonb:
EF maps them as PostgreSQL arrays and compares those by content itself.

## Queries must stay translatable

A query that cannot be translated is either an exception or, worse, silent
client evaluation over the whole table.

The specific trap: `ToLower()` translates to SQL `lower()`; `ToLowerInvariant()`
and the `StringComparison` overloads do **not**. The analyzers will suggest them
anyway, which is why `CharacterRepository.ApplyFilter` carries a targeted
suppression explaining that it must stay as it is.

When adding or changing a query, look at the SQL it generates. Development
configuration has `EnableSensitiveDataLogging` on and logs EF commands at
`Information`.

## Loading a whole entity

A repository that returns a race, class or background with its related rows
defines the includes once, in a private `…WithDetails` query, and every method
starts from it:

```csharp
private IQueryable<Race> RacesWithDetails => _dbContext.Races
    .Include(r => r.AbilityBonuses)
    .Include(r => r.RaceLanguages)
    // ...
    .AsReadOnly();
```

Each method used to repeat the chain, and the copies drifted: none of the race
queries loaded `RaceLanguages`, so every finalized character was missing its race
languages, and the class list left out spellcasting. `AsReadOnly()` is
`AsNoTracking().AsSplitQuery()`, so an extra include costs one more small query,
not a cartesian product.

## Keys

Reference tables use string slugs as primary keys (`"dwarf"`, `"fighter"`,
`"common-phb"`). Join tables use composite keys
(`HasKey(rp => new { rp.RaceId, rp.ProficiencyId })`).

This is why `Entity<TId>` is on `Character` and `CharacterDraft` only: the join
tables have no single id, and the reference entities are deserialized from JSON
by `System.Text.Json`, which needs a public setter that `Entity<TId>`'s
`protected set` would take away.

String slugs are the right key for reference data: they are the same in every
database the seed files produce, readable in URLs and in the ids a character
stores (`"SpellIds": ["guidance"]`), and a Guid would only have to be written
into the JSON by hand to stay stable. Guids are for what users create:
`Character`, `CharacterDraft` and the Identity users. Those are version 7
(`Guid.CreateVersion7` from the injected `TimeProvider`; Npgsql generates v7 for
the users), which start with the creation time, so new rows land at the end of
the primary key index rather than on a random page. At this app's size that
saves nothing measurable; it costs nothing either. Sort by `CreatedAt`, not by
id.

## Seeding

Reference data is loaded from JSON in `Infrastructure/Data` on startup, and
**kept equal to the files**. `SeedService` hashes every file in `Data/`
(`ReferenceDataHash`) and compares the hash with the one row in
`ReferenceDataVersions`. The same hash means nothing to do. A different one means
that, in one transaction, every reference row is deleted, the seeders read the
files in again, and the new hash is recorded. Characters, drafts and users are
not reference data and stay, so a changed seed file reaches a database without
resetting it. A draft loses only the choices the new data drops
(`CharacterDraft.ClearMissingChoices`, the same cascades as `AllowBooks`), each
logged by draft id.

Characters point at their race, subrace, class, subclass and background through
foreign keys, and those rows are deleted and re-added during the replace. The
keys are therefore `NoAction` and `DEFERRABLE INITIALLY IMMEDIATE`, and the
replace runs `SET CONSTRAINTS ALL DEFERRED`: Postgres checks them at commit, when
the same ids are back. A finished sheet is never changed silently, so before the
commit `SeedService` looks for characters naming something the new data drops; if
there is one, it logs each by id and what it is missing, rolls the replace back and
the Api starts on the old data, trying again on the next start. Keep the row, or
move those characters in a migration first. Two details keep this working:

- EF has no setting for deferrable, so the `ReferenceDataSync` migration adds it
  in SQL. A migration that drops and re-adds one of those five keys has to repeat
  it.
- `Restrict` cannot be deferred, only `NoAction`, so the keys stay `NoAction`.

Anything that changes the rows without changing the files, such as a seeder
reading a field differently, needs `ReferenceDataHash.SeederVersion` bumped.
Otherwise existing databases keep the old rows.

The seed files hold plain text. The generator turns 5etools' inline markup into
the text it stands for before it writes them (`{@damage 1d8}` becomes `1d8`,
`{@creature goblin|mm|goblins}` becomes `goblins`; the rules are in its
`FiveEToolsMarkup`), and `SeedDataIntegrityTests` checks no file has any left, so
the database holds plain text and nothing that shows a description has to know
the format.

Every seeder reads its files through `SeedFile.ReadListAsync`, which throws on a
file that is missing, invalid or empty, rather than seeding an empty table. Two
seeders used to fall back to an empty list, which would have left a character
without, say, race languages and said nothing.

Spells load from their subfolder — `Spells/Spells.json` and its companion files —
and everything else from a file at the root of `Data/`. `Races.json` holds each
race with its subraces, traits, bonuses and spells inside it, in place of the nine
hand-made files that were in `Races/`. Every file there is read by a seeder but
two: `SrdIds.json`, which only `SeedDataIntegrityTests` reads, and `Books.json`,
the books every row's `Source` names with their edition and which is each
edition's Player's Handbook, which `BookRepository` reads for the book filter.

Every file but `Proficiencies.json` (`Classes.json`, `Subclasses.json`,
`ClassFeatures.json`, `Races.json`, `Backgrounds.json`, `BackgroundFeatures.json`,
`Feats.json`, `Languages.json`, `Books.json`, `SrdIds.json` and the files in
`Spells/`) is generated from the
raw 5etools data by
`tools/CharacterCreatorApp.SeedDataGenerator` (see the README); don't edit them by
hand, change the generator and run it again. The
row types they share with the seeders are in `Infrastructure/Seed/Rows`. A
class's spell table (cantrips, spells to pick, spell slots or Pact Magic, highest
spell level per level),
its spellcasting ability and how a 2014 one counts its prepared spells, subclass
level, ability score improvement levels and saving throws are all read from the
class data, not written in C#.

The build copies `Data/` next to the app (`CopyToOutputDirectory` in the
Infrastructure project), and `SeedService.DataPath` reads it from
`AppContext.BaseDirectory`, so seeding works the same from Visual Studio, a
publish folder and the container.

## Auditing

Anything implementing `IAuditable` gets `CreatedAt` set on insert and
`UpdatedAt` on update by `AuditableEntityInterceptor`, from the registered
`TimeProvider`. Do not set them by hand.
