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
- The client's services are concrete classes over an HttpClient, so the bUnit tests run the real
  services over `FakeApi`, a strict handler: set up each route a step calls, and assert what it
  sent with `BodyOf<T>`, read as the API binds it. Client.IntegrationTests also needs Chromium,
  which its `WizardHost` installs on the first run (and CI in a step of its own).

## Which tests cover what

Run only the ones a change touches: `--filter-class` takes a pattern, so `"*SeedDataTests"` runs
every seed row test. "Docker" is whether the class starts a container; the seed-JSON and
migration tests live in an integration project but don't.

| Area | Test classes | Project | Docker |
|---|---|---|---|
| Domain rules: drafts, their edits and level-ups, ability scores and increases, feats, races, classes and what a level brings, tools | `CharacterDraftTests`, `CharacterDraftEditTests`, `CharacterDraftBooksTests`, `CharacterDraftFeatAndRaceChoicesTests`, `AbilityScoreTests`, `AbilityScoreMethodTests`, `AbilityIncreaseChoiceTests`, `FeatAbilityIncreaseTests`, `FeatChoiceTests`, `FeatPrerequisitesTests`, `RaceTests`, `ClassTests`, `BackgroundTests`, `ToolPicksTests` | Domain.UnitTests | no |
| Sheet numbers and features | `CharacterStatsCalculatorTests`, `CharacterFeaturesTests`, `CharacterTests` | Domain.UnitTests | no |
| Layering rules from the root CLAUDE.md: Domain has no package, Application no ASP.NET Core or Contracts, the DbContext only in Infrastructure, no repository returns an `IQueryable` | `LayeringTests` (NetArchTest and reflection over the built assemblies; `Program` may name the context to migrate at startup) | Architecture.UnitTests | no |
| Backticked names in every CLAUDE.md still declared in the code | `ClaudeMdReferencesTests` (allowlist inside, each with a reason) | Domain.UnitTests | no |
| Wizard steps (one handler each), hit points among them | `Set*CommandHandlerTests`, `Choose*CommandHandlerTests`, `BonusSpellsHandlersTests`, `ToolChoicesHandlersTests`, `GetSpellAllowanceQueryHandlerTests` | Application.UnitTests | no |
| Finalize (an edit and a level-up too), preview, and opening a character for editing or a level-up | `FinalizeCharacterCommandHandlerTests`, `StartCharacterEditCommandHandlerTests`, `StartCharacterLevelUpCommandHandlerTests`, `FinalizeFeatAndRaceChoicesTests`, `PreviewCharacterQueryHandlerTests`, `BonusSpellsTests`, `CharacterFeatEntriesTests` | Application.UnitTests | no |
| Validators and pipeline | `*ValidatorTests`, `ValidationBehaviourTests` | Application.UnitTests | no |
| Seed JSON content (ids, SRD text, rows per class and race) | `SeedDataIntegrityTests`, `ClassSeedDataTests`, `RaceSeedDataTests`, `BackgroundSeedDataTests`, `FeatSeedDataTests`, `SpellSeedDataTests` | Infrastructure.IntegrationTests | no |
| The character sheet PDF: page 1 on A4 and Letter, entries kept whole, slots | `CharacterSheetPdfRendererTests` (reads the PDF back with PdfPig) | Infrastructure.IntegrationTests | no |
| Migrations (model snapshot in sync) | `MigrationsTests` | Infrastructure.IntegrationTests | no |
| Seeding into the database | `SeedServiceTests` | Infrastructure.IntegrationTests | **yes** |
| Repositories, and the EditCharacter migration's backfill | `CharacterDraftRepositoryTests`, `EditCharacterMigrationTests` | Infrastructure.IntegrationTests | **yes** |
| HTTP endpoints | `CharacterDraftsControllerTests`, `CharactersControllerTests`, `LookupControllersTests`, `UsersControllerTests` | Api.IntegrationTests | **yes** |
| Wizard steps in the client: point buy and the dice roll, improvements and Fighting Styles, spell counts, books, steps filled in from the draft, the marks of an edit, a level-up's flow and hit points; the sheet's Download PDF | `PointBuyComponentTests`, `AbilityScoresStepTests`, `ImprovementsStepTests`, `SpellsStepTests`, `NameAndLevelStepTests`, `SavedChoicesStepTests`, `WizardProgressTests`, `HitPointsStepTests`, `CharacterDetailsTests` | Client.UnitTests | no |
| The wizard in a browser: register, a 2014 and a 2024 character, an edit, two level-ups, a PDF download, a refused pick | `WizardSmokeTests` (Playwright, Chromium) | Client.IntegrationTests | **yes** |
| Seed data generator (5etools parsing, SRD filter) | `FiveEToolsMarkupTests`, `SrdTextTests`, `SourceBooksTests`, `FeatPrerequisiteParserTests`, … | SeedDataGenerator.UnitTests | no |
