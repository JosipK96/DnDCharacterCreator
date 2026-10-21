# Tests

Long form: `docs/conventions/git-and-testing.md`.

- Tests are xUnit v3 + Moq (`MockBehavior.Strict`) + FluentAssertions. The full rules are in
  the private `unit-test-common` skill; the short version is strict mocks, one `_cts` token
  threaded through setups and verifies, pinned arguments, exact assertions, and a row per class
  for any rule that differs by class.
- Tests are `Method_Scenario_Expectation`, Arrange/Act/Assert, xUnit v3 + FluentAssertions +
  Moq. Fake the clock with `FakeTimeProvider`, never `DateTime.Now`.
- A test fixture that cannot produce a draft the validation would accept is telling you the
  fixture is unrealistic, not that the validation is wrong.
- The `*.IntegrationTests` projects start PostgreSQL through Testcontainers, so Docker must be
  running. The unit test projects don't need it.

## Which tests cover what

Run only the ones a change touches: `--filter-class` takes a pattern, so `"*SeedDataTests"` runs
every seed row test. "Docker" is whether the class starts a container; the seed-JSON and
migration tests live in an integration project but don't.

| Area | Test classes | Project | Docker |
|---|---|---|---|
| Domain rules: drafts, ability scores and increases, feats, races, classes, tools | `CharacterDraftTests`, `CharacterDraftBooksTests`, `CharacterDraftFeatAndRaceChoicesTests`, `AbilityScoreTests`, `AbilityScoreMethodTests`, `AbilityIncreaseChoiceTests`, `FeatAbilityIncreaseTests`, `FeatChoiceTests`, `FeatPrerequisitesTests`, `RaceTests`, `ClassTests`, `BackgroundTests`, `ToolPicksTests` | Domain.UnitTests | no |
| Sheet numbers and features | `CharacterStatsCalculatorTests`, `CharacterFeaturesTests`, `CharacterTests` | Domain.UnitTests | no |
| Backticked names in every CLAUDE.md still declared in the code | `ClaudeMdReferencesTests` (allowlist inside, each with a reason) | Domain.UnitTests | no |
| Wizard steps (one handler each) | `Set*CommandHandlerTests`, `Choose*CommandHandlerTests`, `BonusSpellsHandlersTests`, `ToolChoicesHandlersTests`, `GetSpellAllowanceQueryHandlerTests` | Application.UnitTests | no |
| Finalize and preview | `FinalizeCharacterCommandHandlerTests`, `FinalizeFeatAndRaceChoicesTests`, `PreviewCharacterQueryHandlerTests`, `BonusSpellsTests`, `CharacterFeatEntriesTests` | Application.UnitTests | no |
| Validators and pipeline | `*ValidatorTests`, `ValidationBehaviourTests` | Application.UnitTests | no |
| Seed JSON content (ids, SRD text, rows per class and race) | `SeedDataIntegrityTests`, `ClassSeedDataTests`, `RaceSeedDataTests`, `BackgroundSeedDataTests`, `FeatSeedDataTests`, `SpellSeedDataTests` | Infrastructure.IntegrationTests | no |
| Migrations (model snapshot in sync) | `MigrationsTests` | Infrastructure.IntegrationTests | no |
| Seeding into the database | `SeedServiceTests` | Infrastructure.IntegrationTests | **yes** |
| Repositories | `CharacterDraftRepositoryTests` | Infrastructure.IntegrationTests | **yes** |
| HTTP endpoints | `CharacterDraftsControllerTests`, `CharactersControllerTests`, `LookupControllersTests`, `UsersControllerTests` | Api.IntegrationTests | **yes** |
| Seed data generator (5etools parsing, SRD filter) | `FiveEToolsMarkupTests`, `SrdTextTests`, `SourceBooksTests`, `FeatPrerequisiteParserTests`, … | SeedDataGenerator.UnitTests | no |
