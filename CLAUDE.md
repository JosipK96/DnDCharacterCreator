# CLAUDE.md

.NET 10 D&D 5E character creator: an ASP.NET Core API and a Blazor WebAssembly client over
PostgreSQL, one solution, clean architecture. Setup and how to run it: see README.md.

## Build & test

- `dotnet build CharacterCreatorApp.sln`, `dotnet test --project <Tests.csproj>`, or
  `--filter-class "*.<Class>"` for one class. Tests run on Microsoft.Testing.Platform
  (`global.json`'s `test` runner, xunit.v3 4), so VSTest's `--filter "FullyQualifiedName~..."`
  and a positional project path no longer work.
- `TreatWarningsAsErrors` is on, with StyleCop, AsyncFixer and `EnforceCodeStyleInBuild`. A
  change is not done until it builds clean. Don't suppress to get green: `analyzers/
  GlobalSuppressions.cs` holds formatting-only rules and a few named domain-vocabulary
  collisions, and anything you add there needs a justification that is not "the build failed".
- The `*.IntegrationTests` projects start PostgreSQL through Testcontainers, so Docker must be
  running. The unit test projects don't need it.
- Package versions go in `Directory.Packages.props` only; shared build settings in
  `Directory.Build.props` (and `tests/Directory.Build.props`, which inherits and relaxes it).
  Never put a `Version=` on a `PackageReference`.
- MediatR stays on 12.4.1: 13 and later need a paid or registered license in production, and
  12.5.0 changes the pipeline delegate. Dependabot ignores it; move it only by hand.
- CI (`.github/workflows/ci.yml`) runs on every pull request and push to `main`: restore,
  build (warnings as errors), unit tests, integration tests and a vulnerable-package check as
  separate steps, and the Api's Docker image (skipped when a PR changes only Markdown or
  `docs/`). Branch protection requires both jobs before a PR can merge: once reviewed, turn on
  auto-merge (`gh pr merge <n> --auto --squash`) and GitHub merges it when they pass.
  Dependabot proposes NuGet updates weekly and Actions monthly; a workflow turns on auto-merge
  for its minor and patch updates, while major ones wait for a review.

## Where code goes

- `src/{Domain, Application, Infrastructure, Api, Contracts, Client}`, tests in `tests/`.
- The Application layer is CQRS over MediatR: use `ICommand`/`ICommandHandler` and
  `IQuery`/`IQueryHandler` from `Common/Abstractions`, not raw `IRequest`. One handler per file
  under `Features/<Area>/<UseCase>/`.
- Inside Application, `Features/` holds only use cases and what a single feature area owns.
  Anything two areas share sits beside it: `Persistence/` (`IUnitOfWork` and the repository
  interfaces), `ReadModels/`, `Errors/` (the shared `<Area>Errors`), and `Common/` for plumbing
  that knows nothing about D&D (CQRS abstractions, pipeline behaviours, filtering, `IUserContext`).
- Application does not reference Contracts, and there is no project reference to let it.
  Controllers map a `*ApiRequest` onto a command or query, and map what comes back (a read
  model, `PagedResult<T>`, `LoginResult`) onto a `*ApiResponse`. List queries take
  `FilterOptions<T>` from `Common/Filtering`.
- Handlers orchestrate; rules and state transitions live on the domain model. `Character` and
  `CharacterDraft` derive from `Entity<TId>` and expose intent-named methods
  (`ApplyAbilityScoreImprovements`, `SetRaceSkills`), never public setters on the interesting
  state.
- ASP.NET Core Identity stays in Infrastructure: `ApplicationUser` and `IdentityService` are in
  `Infrastructure/Identity`, and Application reaches them through `IIdentityService`. Domain has
  no package references, and Application has no ASP.NET Core reference.
- `DbContext` stays in Infrastructure behind repositories. A repository returns either a domain
  entity (to change) or a `*ReadModel` (to read), never an `IQueryable`. Read models are
  Application types, in `ReadModels/`; the Domain does not use them.
- A repository that loads a whole race, class or background writes its includes once, in a
  private `…WithDetails` query every method starts from. Per-method copies drifted, and every
  finalized character lost its race languages.
- Shared creation logic lives in `Features/CharacterCreation/Building`: `CharacterBuilder` turns
  a draft into a `Character`, `DraftCompletenessChecker` says what is still missing,
  `DraftSourceLoader` fetches the entities a draft names by id. Finalize and preview both use
  all three — that is the point, so the review step cannot disagree with what finalize builds.
- Derived sheet numbers (hit points, armour class, saves, skills) come from
  `Domain/Services/CharacterStatsCalculator`. Don't recompute a modifier in a Razor page. A
  character's features aren't stored either: `CharacterFeatures` works them out from class,
  subclass, level and background, so regenerated seed data never leaves a sheet with dead ids.
- The Api serves the Blazor client (`MapStaticAssets` + `MapFallbackToFile`), so the client
  calls the API on its own origin: no CORS, no client-side API address. Run the Api to use it.
- The wizard's step order, numbers and names come from `Client/Pages/CharacterCreation/
  WizardSteps`; a step reads its heading and "Next" label from `WizardStepBase`, never a literal.

## Errors and validation  → docs/conventions/errors-and-validation.md

- Every error code is a static method on an `<Area>Errors` class, namespaced
  `"<Area>.<Reason>"`. Never write `Error.Validation("Some.Code", ...)` at a call site.
- Request-shape rules go in a FluentValidation validator, which `ValidationBehaviour`
  (`Application/Common/Behaviours`) runs in the pipeline. Every rule sets `.WithErrorCode(...)`
  from the `Codes` class nested in the matching `<Area>Errors`; without it the code is
  FluentValidation's validator name (`NotEmptyValidator`).
- Anything needing the database is a handler check, not a validator.

## D&D rules  → docs/conventions/ability-scores-and-ids.md

- A character is built with one rules edition, 2014 or 2024 (`RulesEdition`), picked in step 1:
  `CharacterDraft.ChooseEdition` clears every choice when it changes, because the editions share
  no ids (the 2024 wizard is `wizard-2024`, the 2014 one `wizard`). Lookups take `?edition=`,
  and a step handler refuses an entity from the other edition with
  `CharacterDraftErrors.EditionMismatch`. Classes, races, backgrounds, spells, feats and the
  languages a player picks are all split by edition (a reprint is a second spell,
  `fireball_2024`).
- A 2024 species is a race (`elf_2024`), and its lineages, ancestries and legacies are its
  subraces (`elf_drow_lineage_2024`, `dragonborn_black_2024`). It has no ability increases or
  languages; those come with the 2024 background. A race and its subrace give spells from a
  character level (`Race.SpellIdsAt`, added by `CharacterBuilder`), and a subrace may change
  the speed (`Race.SpeedWith`) and add language picks (`Race.LanguageChoiceCountWith`).
- A 2024 background raises three abilities, +2 and +1 or +1 to each, picked in the background
  step (`Background.AllowsAbilityIncreases`, kept in `CharacterDraft.BackgroundAbilityIncreases`),
  and gives an origin feat (`Background.FeatId`, which some 2014 ones have too); it has no
  feature. `CharacterBuilder` applies both, and a feat taken for an improvement may not repeat
  it. The 2024 rules' Common and two standard languages for every character are carried on the
  2024 backgrounds (`["common", "any", "any"]`), so the language step needs nothing else.
- Every picked increase is an `AbilityIncreaseChoice` (points over options, at most so many per
  ability): a 2024 background's, and a race's or subrace's +1s (`Race.AbilityChoiceWith`: a
  half-elf's two, a variant human's two of six), kept in `CharacterDraft.RaceAbilityIncreases`.
  The 2014 human is a standard or a variant one, subraces the generator makes from 5etools'
  nameless one beside the named one, and a race with subraces needs one at finalize. A race's
  own feats (`Race.BonusFeatsWith`: the variant human's any one, the 2024 human's Origin one)
  come first in `SelectedFeatIds`. What a feat raises is `Feat.AbilityIncrease` (up to 20, 30
  for an Epic Boon); the pick is in `CharacterDraft.FeatAbilityIncreases`, one per feat in the
  same order, and `FeatAbilityIncrease.Resolve` turns it into the increase (a fixed one needs none).
- A subclass gives spells two ways (`SpellSubclass.IsAlwaysPrepared`): domain and oath spells
  come with it at their level (`Subclass.AlwaysPreparedSpellIdsAt`, added by `CharacterBuilder`),
  and a 2014 patron's only join the list the warlock picks from. `Subclass.SpellListClassId`
  opens a whole other list: the Wizard's for an Eldritch Knight, whose fighter has none. The
  spells step and `ChooseSpells` both take all three into account.
- A class's rules (`Class.SubclassLevel`, `Class.AsiLevels`, `Class.SavingThrowAbilityIds`,
  the spell table) and a race's (`Race.SkillChoiceCount` and its options, `Race.
  LanguageChoiceCount`) are read from the 5etools data by the generator. Read them from the
  entity and never hardcode one at a call site — two separate bugs came from assuming every
  class picks a subclass at level 3. `ClassSeedDataTests` and `RaceSeedDataTests` pin the
  values to the book, a row per class and race.
- Ability keys arrive as `STR`, `str` and `strength` depending on the source; always go through
  `AbilityScore.Normalize`/`TryNormalize`. `Character.AbilityScores` only ever holds canonical
  ids.
- Base scores come with the method that made them (`AbilityScoreMethod`: the standard array,
  27-point buy with 8 to 15, or 3 to 18 rolled), and the server checks they fit it, in the
  validator and again in `DraftCompletenessChecker`. A draft from before the method was kept
  has to pick its scores again.
- Language ids are source-suffixed (`common-phb`, `common-xphb`): a 2014 character's are the
  PHB's, a 2024 one's the 2024 book's (`Language.SourceFor`). Race languages are stored as
  display names and background languages contain `"any"` placeholders; `LanguageResolver`
  resolves all three against the edition's languages, for `CharacterBuilder` and
  `ChooseLanguages` alike. What a player may pick is `ILanguageRepository.GetChoosableAsync`
  (`Language.ChoosableTypes`: any PHB language but the secret ones in 2014, the standard ones
  in 2024), and a pick the race or background already gives is refused.
- Armour is not modelled, so armour class is the unarmoured 10 + DEX and the sheet says so.
- Hit points some choices add every level (Dwarven Toughness, a 2014 hill dwarf's and every
  2024 dwarf's; Draconic Resilience; Tough) are in `Domain/Services/HitPointBonuses`.
  `CharacterStatsInput` has no defaults, so the preview and the sheet cannot build it
  differently.
- Whether a proficiency is a skill is `Skill.IsSkill`, never a comparison with its `Type`
  string: comparing with `"Skill"` against the data's `"skill"` locked half-elves out.
- A feat belongs to one edition (`Feat.Edition`; 2024 ones have a `Category`, "Origin" or
  "Epic Boon"). One in both keeps its book's name in each, and its id says which:
  `alert_2014`, `alert_2024`. Who may take one is `Feat.Prerequisites.FindUnmet`: level,
  ability minimums (racial bonuses count, and so does every improvement for a feat taken for
  one, but not for a race's own feat), race, class, and class features by name ("Fighting
  Style", "Spellcasting" or "Pact Magic", from `CharacterFeatures.ClassFeaturesFor`). What it
  cannot check (another feat, a campaign) is shown, not enforced. Classes, subclasses, their
  features, races, backgrounds, spells and feats are generated from 5etools; Tasha's optional
  class features, and the choices the app does not ask for yet (a dwarf's tools, a
  background's gaming set, Resilient's saving throw), are left out.

## Data and migrations  → docs/conventions/efcore-and-postgres.md

- Reference data is seeded from JSON in `Infrastructure/Data` on startup. When the files' hash
  differs from `ReferenceDataVersions`, `SeedService` deletes every reference row and reads the
  files again in one transaction; characters, drafts and users stay. A character's race, class
  and background keys are deferrable `NoAction` for that, set in SQL by the `ReferenceDataSync`
  migration, so a migration that re-creates one of them must repeat it. Bump
  `ReferenceDataHash.SeederVersion` when rows change without the files changing.
- The seed files hold plain text: the generator's `FiveEToolsMarkup` takes 5etools markup
  (`{@damage 1d8}`) out of every string before it writes one, and `SeedDataIntegrityTests`
  checks none is left. Seeders read them through `SeedFile.ReadListAsync`, which refuses an
  empty file. The database holds plain text; don't render markup anywhere downstream.
- Only SRD text (5.1 and 5.2, CC-BY 4.0; attribution in NOTICE.md) may be in the seed files.
  The generator's `SrdText` keeps a description only for an entry 5etools marks `srd`/`srd52`
  and lists those ids in `SrdIds.json`; every other one gets its mechanics and a book reference,
  and a non-SRD spell no material wording. `SeedDataIntegrityTests` fails on book prose outside
  that list. Never paste book text into a seed file or the generator.
- After regenerating or editing the seed JSON, run `SeedDataIntegrityTests` (Infrastructure
  tests, no database): it catches one thing under two ids (`thieves_tools`/`thieves-tools`)
  and ids a feat or background names that nothing defines.
- `Character` and `CharacterDraft` ids are UUIDv7 (`Guid.CreateVersion7` from the injected
  `TimeProvider`); Identity's user ids are v7 from Npgsql. Sort by `CreatedAt`, not by id.
- Migrations: `dotnet ef migrations add <Name> --startup-project ..\CharacterCreatorApp.Api\
  CharacterCreatorApp.Api.csproj` from the Infrastructure project. **Read what it scaffolds**:
  it emitted `defaultValue: ""` for a `jsonb` column once, which Postgres rejects.
- Lists and dictionaries on `Character` are `jsonb` via `HasJsonConversion`.
- Anything implementing `IAuditable` gets `CreatedAt`/`UpdatedAt` stamped by
  `AuditableEntityInterceptor`; don't set them by hand.

## Configuration

- The JWT signing key is never in appsettings: user secrets in development, `JWT_KEY` in the
  gitignored `.env` for docker compose (template: `.env.example`), `JwtSettings__Key` on a host.
  The only connection string in appsettings is the Development one, for the compose database
  on `localhost:5433`; elsewhere it comes from `ConnectionStrings__DefaultConnection`. The app
  refuses to start without either.
- `docker-compose.yml` (+ `.override.yml`, `.dcproj`) sits at the repository root because the
  Dockerfile builds from there: restore needs `Directory.*.props`, `.editorconfig` and
  `analyzers/`. Non-secret container settings are in `configurations/Env_files/api.env`.
- `Program.cs` registers services while the `WebApplicationBuilder` is built, which is before
  `WebApplicationFactory` applies test configuration — which is why the API integration tests
  pass config through environment variables.

## Code conventions  → docs/conventions/coding-standards.md

- Primary constructors preferred; assign parameters to `private readonly _fields` and use the
  field.
- Always braces after `if`. Prefer early return over `else` nesting.
- Async methods crossing a boundary take and forward a `CancellationToken`. No `.Result`/
  `.Wait()`. Existing code uses the `Async` suffix — keep it, don't start a rename.
- Comments explain *why*. No commented-out code: a commented-out test body passed vacuously for
  a long time before anyone noticed.
- Tests are `Method_Scenario_Expectation`, Arrange/Act/Assert, xUnit v3 + FluentAssertions +
  Moq. Fake the clock with `FakeTimeProvider`, never `DateTime.Now`.
- A test fixture that cannot produce a draft the validation would accept is telling you the
  fixture is unrealistic, not that the validation is wrong.

## Git and tests  → docs/conventions/git-and-testing.md

- Work arrives through pull requests: a branch per issue, a PR that starts with `Closes #N`
  and does only what the issue asks, merged when CI is green. Never push to `main` directly.
  The details are in `docs/agents/issue-tracker.md`.
- Commit messages: plain sentences saying what changed and why. No `feat(...)`/`fix(...)`
  prefixes.
- Tests are xUnit v3 + Moq (`MockBehavior.Strict`) + FluentAssertions. The full rules are in
  the private `unit-test-common` skill; the short version is strict mocks, one `_cts` token
  threaded through setups and verifies, pinned arguments, exact assertions, and a row per class
  for any rule that differs by class.

## Skills

`/unit-test-cases`, `/unit-test-write`, `/unit-test-review`, `/pr-review`, `/user-story`,
`/release-labels`. They are private and not part of this repository: `.claude/` is ignored, and
`.claude/skills` is a clone of a separate private repository. Without it, the conventions in
`docs/conventions` still apply.
Domain terms: docs/conventions/glossary.md.

## Agent skills

### Issue tracker

Issues live in the repo's GitHub Issues (JosipK96/DnDCharacterCreator), through the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context: the glossary is `docs/conventions/glossary.md` (and `CONTEXT.md` once one exists), decisions in `docs/adr/`. See `docs/agents/domain.md`.

## Don't

- Don't edit `src/.claude/worktrees/` — it's ignored, and it is not the code that builds.
- Don't add a project without adding it to `CharacterCreatorApp.sln`.
- Don't hand-edit a seed file the generator writes: every one but `Proficiencies.json`, which it
  reads to name what the others grant. The next run overwrites it. Change `tools/
  CharacterCreatorApp.SeedDataGenerator` and run it again.
