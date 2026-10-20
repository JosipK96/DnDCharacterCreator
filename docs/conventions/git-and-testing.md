# Git and testing workflow

## Git

- Commit only when asked. Never push — that is the developer's call.
- Commit messages are plain sentences saying what changed and why. No
  `feat(...)` / `fix(...)` prefixes, no padded bodies.
- Say what the change *fixes*, not just what it touches. "Apply ASIs, chosen
  languages and race skills when finalizing a character" is useful a year later;
  "update handler" is not.
- `main` should stay buildable. With `TreatWarningsAsErrors` on, "builds" means
  zero warnings.
- Do not commit while something still needs a look — a generated migration, for
  instance.

## What is ignored

All of `.claude/`: the Claude Code skills (a clone of a separate private
repository), its worktrees and its local settings. The worktrees directory is a
Claude Code working area, not code that builds; editing anything in there has
no effect on the solution.

## Running the tests

```bash
dotnet test --solution CharacterCreatorApp.sln                     # everything
dotnet test --project tests/CharacterCreatorApp.Domain.UnitTests
dotnet test --project <Tests.csproj> --filter-class "*.FinalizeCharacterCommandHandlerTests"
dotnet test --project <Tests.csproj> --filter-method "*.Handle_WithACompleteDraft*"
```

`global.json` runs `dotnet test` on Microsoft.Testing.Platform (xunit.v3 4), so projects and
solutions are passed with `--project`/`--solution`, and xunit's `--filter-class`,
`--filter-method` and `--filter-trait` replace VSTest's `--filter "FullyQualifiedName~..."`.

| Project | Needs Docker |
|---|---|
| `CharacterCreatorApp.Domain.UnitTests` | no |
| `CharacterCreatorApp.Application.UnitTests` | no |
| `CharacterCreatorApp.Infrastructure.IntegrationTests` | **yes** — Testcontainers starts PostgreSQL |
| `CharacterCreatorApp.Api.IntegrationTests` | **yes** |

Which tests cover what, so a change runs only its own (`--filter-class` takes a pattern, so
`"*SeedDataTests"` runs every seed row test):

| Area | Test classes | Project | Docker |
|---|---|---|---|
| Domain rules: drafts, ability scores and increases, feats, races, classes, tools | `CharacterDraftTests`, `CharacterDraftBooksTests`, `CharacterDraftFeatAndRaceChoicesTests`, `AbilityScoreTests`, `AbilityScoreMethodTests`, `AbilityIncreaseChoiceTests`, `FeatAbilityIncreaseTests`, `FeatPrerequisitesTests`, `RaceTests`, `ClassTests`, `BackgroundTests`, `ToolPicksTests` | Domain.UnitTests | no |
| Sheet numbers and features | `CharacterStatsCalculatorTests`, `CharacterFeaturesTests`, `CharacterTests` | Domain.UnitTests | no |
| Wizard steps (one handler each) | `Set*CommandHandlerTests`, `Choose*CommandHandlerTests`, `BonusSpellsHandlersTests`, `ToolChoicesHandlersTests` | Application.UnitTests | no |
| Finalize and preview | `FinalizeCharacterCommandHandlerTests`, `FinalizeFeatAndRaceChoicesTests`, `PreviewCharacterQueryHandlerTests`, `BonusSpellsTests` | Application.UnitTests | no |
| Validators and pipeline | `*ValidatorTests`, `ValidationBehaviourTests` | Application.UnitTests | no |
| Seed JSON content (ids, SRD text, rows per class and race) | `SeedDataIntegrityTests`, `ClassSeedDataTests`, `RaceSeedDataTests`, `BackgroundSeedDataTests`, `FeatSeedDataTests`, `SpellSeedDataTests` | Infrastructure.IntegrationTests | no |
| Migrations (model snapshot in sync) | `MigrationsTests` | Infrastructure.IntegrationTests | no |
| Seeding into the database | `SeedServiceTests` | Infrastructure.IntegrationTests | **yes** |
| Repositories | `CharacterDraftRepositoryTests` | Infrastructure.IntegrationTests | **yes** |
| HTTP endpoints | `CharacterDraftsControllerTests`, `CharactersControllerTests`, `LookupControllersTests`, `UsersControllerTests` | Api.IntegrationTests | **yes** |
| Seed data generator (5etools parsing, SRD filter) | `FiveEToolsMarkupTests`, `SrdTextTests`, `SourceBooksTests`, `FeatPrerequisiteParserTests`, … | SeedDataGenerator.UnitTests | no |

If the integration tests fail with `DockerUnavailableException`, start Docker
Desktop and wait for the daemon rather than assuming the tests are broken.

## Running the app

The API needs its two secrets (see README.md) and a PostgreSQL instance. On
startup it applies pending migrations and seeds reference data, so a first run
against an empty database does the setup for you.

## The testing rules

The full set, each rule with its own impact rating and checklist, is in the
private `unit-test-common` skill, outside this repository. The three skills that
use them:

- `/unit-test-cases <target>` — enumerate what needs covering, write nothing
- `/unit-test-write <target>` — confirm behaviours, then the mock receipt, then write
- `/unit-test-review <target>` — check existing tests against the rules

The short version: strict mocks, one `_cts` token threaded through, pin the
arguments the SUT forwards, assert exact values, and a rule that differs per
class gets a row per class.
