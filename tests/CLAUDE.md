# Tests

Long form: `docs/conventions/git-and-testing.md`.

- Tests are xUnit v3 + Moq + FluentAssertions, named `Method_Scenario_Expectation`, in
  Arrange/Act/Assert.

## Writing a unit test

- **Mock only what reaches the database or the request:** repositories, `IUnitOfWork`,
  `IUserContext`. Everything in-process runs real (`CharacterBuilder`,
  `DraftCompletenessChecker`, `CharacterStatsCalculator`, `DraftSourceLoader` over the
  repository mocks). The clock is a `FakeTimeProvider`, never a mock and never `DateTime.Now`.
- **Every mock is `MockBehavior.Strict`, and forwarded values are pinned.** Set up and verify
  with the real draft id, user id and one `_cts.Token`, not `It.IsAny`: the user id is how the
  repositories scope every read, so a test that ignores it passes when the handler reads another
  player's draft. `It.IsAny` belongs only on a `Times.Never` verify, where it is the stronger check.
- **Assert the exact value.** A field gets its expected value, an error its **code** (codes are
  the contract, descriptions get reworded), never `NotBeNull()` or `NotBeEmpty()` alone.
- **One case per distinct outcome, and a row per value a rule distinguishes.** A rule that
  differs by class or race (`Class.SubclassLevel`, `Class.ImprovementsAt`) gets an
  `[InlineData]` row for each distinct value: two bugs survived a single fighter-shaped test.
  Several guards returning the same code are one `[Theory]`, a row per guard.
- **Report a defect; don't pin it.** A test that asserts an accident (a
  `NullReferenceException` from a `!`, the wrong error code for a step) makes the bug the
  contract. Write it down with file and line instead, and fix it in its own change. A test with
  no assertion or a commented-out body is a defect too.
- **Append to the existing `<Sut>Tests` class** and follow its local conventions. Search first:
  a class with no test file of its own may be covered through a handler's tests (the
  `CharacterCreation/Shared` helpers are, through the step handlers, finalize and preview).
- A test fixture that cannot produce a draft the validation would accept is telling you the
  fixture is unrealistic, not that the validation is wrong.

## What the test projects need

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
| Domain rules: drafts, their edits and level-ups, ability scores and increases, feats, races, classes and what a level brings, tools | `CharacterDraftTests`, `CharacterDraftEditTests`, `CharacterDraftBooksTests`, `CharacterDraftFeatAndRaceChoicesTests`, `AbilityScoreTests`, `AbilityScoreMethodTests`, `AbilityIncreaseChoiceTests`, `FeatAbilityIncreaseTests`, `FeatCategoryTests`, `FeatChoiceTests`, `FeatPrerequisitesTests`, `RaceTests`, `ClassTests`, `BackgroundTests`, `ToolPicksTests` | Domain.UnitTests | no |
| Sheet numbers and features | `CharacterStatsCalculatorTests`, `CharacterStatsInputTests`, `CharacterFeaturesTests`, `CharacterTests` | Domain.UnitTests | no |
| Layering rules from the root CLAUDE.md: Domain has no package, Application no ASP.NET Core or Contracts, the DbContext only in Infrastructure, no repository returns an `IQueryable` | `LayeringTests` (NetArchTest and reflection over the built assemblies; the Api entry point may name the context to migrate at startup) | Architecture.UnitTests | no |
| Backticked names in every CLAUDE.md, the README and `docs/` still declared in the code | `ClaudeMdReferencesTests` (allowlist inside, each with a reason) | Domain.UnitTests | no |
| Wizard steps (one handler each), hit points among them | `Set*CommandHandlerTests` (`SetCharacterNameAndLevelCommandHandlerTests` among them), `Choose*CommandHandlerTests`, `BonusSpellsHandlersTests`, `ToolChoicesHandlersTests`, `GetFightingStylesQueryHandlerTests`, `GetFeatEligibilityQueryHandlerTests`, `GetSpellAllowanceQueryHandlerTests` | Application.UnitTests | no |
| Finalize (an edit and a level-up too), preview, the saved sheet, and opening a character for editing or a level-up | `FinalizeCharacterCommandHandlerTests`, `StartCharacterEditCommandHandlerTests`, `StartCharacterLevelUpCommandHandlerTests`, `FinalizeFeatAndRaceChoicesTests`, `PreviewCharacterQueryHandlerTests`, `BonusSpellsTests`, `BonusSpellcastingTests`, `CharacterFeatEntriesTests`, `CharacterSheetContentLoaderTests`, `CharacterDetailsLoaderTests`, `GetCharacterDetailsQueryHandlerTests`, `GetCharacterSheetPdfQueryHandlerTests` | Application.UnitTests | no |
| Deleting a draft or a character | `DeleteCharacterDraftCommandHandlerTests`, `DeleteCharacterCommandHandlerTests` | Application.UnitTests | no |
| Validators and pipeline | `*ValidatorTests`, `ValidationBehaviourTests`, `LoggingBehaviourTests` | Application.UnitTests | no |
| Sign-in, registration and password recovery handlers | `LoginUserCommandHandlerTests`, `RegisterUserCommandHandlerTests`, `PasswordRecoveryHandlersTests` | Application.UnitTests | no |
| Lookups and read models | `GetRacesQueryHandlerTests`, `GetHiddenOptionsQueryHandlerTests`, `SpellReadModelTests`, `PageOptionsTests` | Application.UnitTests | no |
| Shared plumbing: the response type and the entity base | `ResponseTests`, `EntityTests` | Domain.UnitTests | no |
| Seed JSON content (ids, SRD text, rows per class and race) | `SeedDataIntegrityTests`, `ClassSeedDataTests`, `RaceSeedDataTests`, `BackgroundSeedDataTests`, `FeatSeedDataTests`, `SpellSeedDataTests` | Infrastructure.IntegrationTests | no |
| The character sheet PDF: page 1 on A4 and Letter, entries kept whole, slots, spell save DC and attack bonus | `CharacterSheetPdfRendererTests` (reads the PDF back with PdfPig) | Infrastructure.IntegrationTests | no |
| Migrations (model snapshot in sync) | `MigrationsTests` | Infrastructure.IntegrationTests | no |
| Seeding into the database | `SeedServiceTests` | Infrastructure.IntegrationTests | **yes** |
| Repositories, and the EditCharacter migration's backfill | `CharacterDraftRepositoryTests`, `EditCharacterMigrationTests` | Infrastructure.IntegrationTests | **yes** |
| Error reporting (#77): what the scrubber removes, the client-error endpoint, the owner's test error with the version, path and trace id on the event | `ErrorScrubberTests`, `ClientErrorReporterTests` (no Docker), `ErrorReportingTests`, `ThrowTestErrorCommandHandlerTests` (Application.UnitTests) | Api.IntegrationTests | `ErrorReportingTests` **yes** |
| HTTP endpoints | `CharacterDraftsControllerTests`, `CharactersControllerTests`, `LookupControllersTests`, `UsersControllerTests`, `PasswordRecoveryTests` (reset links, reset, change password) | Api.IntegrationTests | **yes** |
| Wizard steps in the client: point buy and the dice roll, improvements, which feats can be taken and Fighting Styles, spell counts, books and each step's note on the unticked ones (`HiddenOptionsNoteTests`, with the wizard's return from step 1), steps filled in from the draft, Create character asking to resume an unfinished draft only once step 1 is saved, the progress map's rooms and phone form, the marks of an edit, a level-up's flow and hit points, and a new character's above level 1; the review step's and the sheet's spell save DC and attack bonus; the sheet's numbers, proficiency marks, delete dialog and Download PDF; the list's rooms, empty state, paging and delete dialog; unhandled errors sent to the API; the class step's rooms and key; the race step's ability increases and same-named races; the shared message, room, choice and spell slot pieces | `ClientErrorLoggerProviderTests`, `PointBuyComponentTests`, `AbilityScoresStepTests`, `ScoreAssignmentTests`, `RaceStepTests`, `ImprovementsStepTests`, `DraftImprovementsTests` (the slots rebuilt from a saved draft), `WizardNavigatorTests` (the step Create character opens at and where each button goes), `SpellsStepTests`, `ClassStepTests`, `NameAndLevelStepTests`, `SavedChoicesStepTests`, `ResumeDraftPromptTests`, `WizardProgressTests`, `HitPointsStepTests`, `ReviewStepTests`, `CharacterDetailsTests`, `CharacterListTests`; the password pages: `PasswordPagesTests`; the shared pieces: `MessageTests`, `RoomTests`, `ChoicePiecesTests`, `SpellSlotsPanelTests`; the update prompt: `UpdatePromptTests` | Client.UnitTests | no |
| The wizard in a browser: register, a 2014 and a 2024 character, an edit, two level-ups, a PDF download, a refused pick; and a 375×812 phone run of the wizard and the sheet that fails on sideways scroll or a tap target under 44×44px (`Wizard_OnAPhone_ShowsTheCompactMapNeverScrollsSidewaysAndKeepsTapTargetsBigEnough`) | `WizardSmokeTests` (Playwright, Chromium) | Client.IntegrationTests | **yes** |
| The installable app: `index.html` links the manifest, the manifest's name, start URL, display and icons | `InstallableAppTests` | Client.IntegrationTests | **yes** |
| Seed data generator (5etools parsing, SRD filter, copies, race shapes) | `FiveEToolsMarkupTests`, `SrdTextTests`, `SourceBooksTests`, `FeatPrerequisiteParserTests`, `FiveEToolsCopyTests`, `RaceImporterTests`, … | SeedDataGenerator.UnitTests | no |
