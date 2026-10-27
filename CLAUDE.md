# CLAUDE.md

.NET 10 D&D 5E character creator: an ASP.NET Core API and a Blazor WebAssembly client over
PostgreSQL, one solution, clean architecture. Setup and how to run it: see README.md.

## Where the rest is

Each project has its own CLAUDE.md, loaded when you read a file in it. Sessions start in `src/`,
so the two outside it never load on their own: read them before working there.

- `src/CharacterCreatorApp.Domain/CLAUDE.md`: the domain model and the rules that caused bugs;
  each D&D rule at length (editions, books, races, classes, backgrounds, feats, spells,
  languages, tools, hit points) is in `docs/conventions/dnd-rules.md`.
- `src/CharacterCreatorApp.Application/CLAUDE.md`: CQRS, character building, errors and validation.
- `src/CharacterCreatorApp.Infrastructure/CLAUDE.md`: seed data, migrations, repositories, Identity.
- `src/CharacterCreatorApp.Api/CLAUDE.md`: controllers, configuration and secrets.
- `src/CharacterCreatorApp.Client/CLAUDE.md`: the wizard.
- `tests/CLAUDE.md`: test conventions and which test class covers which area (and needs Docker).
- `tools/CharacterCreatorApp.SeedDataGenerator/CLAUDE.md`: 5etools data and the SRD-only rule.
- `docs/conventions/` explains each convention at length; domain terms are in its `glossary.md`.

The rules nearly every task needs:

- A character has one rules edition, 2014 or 2024 (`RulesEdition`). The editions share no ids:
  lookups take `?edition=`, and a step handler refuses an entity from the other edition with
  `CharacterDraftErrors.EditionMismatch`.
- Ability keys always go through `AbilityScore.Normalize`/`TryNormalize`;
  `Character.AbilityScores` only ever holds canonical ids.
- Read a class's and race's rules from the entity (`Class.SubclassLevel`, `Class.AsiLevels`,
  `Race.SkillChoiceCount`), never hardcode one at a call site.
- Whether a proficiency is a skill is `Skill.IsSkill`, never a comparison with its `Type` string.

## Build & test

- `dotnet build CharacterCreatorApp.sln`, `dotnet test --project <Tests.csproj>`, or
  `--filter-class "*.<Class>"` for one class. Tests run on Microsoft.Testing.Platform
  (`global.json`'s `test` runner, xunit.v3 4), so VSTest's `--filter "FullyQualifiedName~..."`
  and a positional project path no longer work.
- Run only the test classes a change touches (the table in `tests/CLAUDE.md` says which), not
  whole test projects: CI runs the full suite on the PR. Run more only for a change that crosses
  layers, such as a seed data, migration or shared-type change. Build with `-v q -nologo` so
  only warnings and errors come back.
- `TreatWarningsAsErrors` is on, with StyleCop, AsyncFixer and `EnforceCodeStyleInBuild`. A
  change is not done until it builds clean. Don't suppress to get green: `analyzers/
  GlobalSuppressions.cs` holds formatting-only rules and a few named domain-vocabulary
  collisions, and anything you add there needs a justification that is not "the build failed".
- Package versions go in `Directory.Packages.props` only; shared build settings in
  `Directory.Build.props` (and `tests/Directory.Build.props`, which inherits and relaxes it).
  Never put a `Version=` on a `PackageReference`.
- CI (`.github/workflows/ci.yml`) runs on every pull request and push to `main`: restore,
  build (warnings as errors), unit tests, integration tests, test coverage (with a floor for Domain and
  Application, `docs/conventions/git-and-testing.md`) and a vulnerable-package check as
  separate steps, and the Api's Docker image (skipped when a PR changes only Markdown or
  `docs/`). `.github/workflows/codeql.yml` scans the C# code for security flaws (not a required
  check; its alerts are under the Security tab). Branch protection requires both jobs before a PR can merge: once reviewed, turn on
  auto-merge (`gh pr merge <n> --auto --squash`) and GitHub merges it when they pass.

## Where code goes

- `src/{Domain, Application, Infrastructure, Api, Contracts, Client}`, tests in `tests/`.
- Handlers orchestrate; rules and state transitions live on the domain model.
- Domain has no package references, and Application has no ASP.NET Core reference.
  Application does not reference Contracts, and there is no project reference to let it.
- `DbContext` stays in Infrastructure behind repositories. A repository returns either a domain
  entity (to change) or a `*ReadModel` (to read), never an `IQueryable`. Read models are
  Application types, in `ReadModels/`; the Domain does not use them.

## Code conventions  → docs/conventions/coding-standards.md

- Make the smallest change that fully does the issue, reusing what the codebase has; never cut
  validation, error handling or security to get there. A known limit you accept gets a comment:
  `// shortcut: <the limit>, <when to upgrade>`.

- Primary constructors preferred; assign parameters to `private readonly _fields` and use the
  field.
- Always braces after `if`. Prefer early return over `else` nesting.
- Async methods crossing a boundary take and forward a `CancellationToken`. No `.Result`/
  `.Wait()`. Existing code uses the `Async` suffix — keep it, don't start a rename.
- Comments explain *why*. No commented-out code: a commented-out test body passed vacuously for
  a long time before anyone noticed.

## Before a pull request  → docs/conventions/coding-standards.md

Check the change against these before opening it. Each one was a finding of the 1.0 code-quality
audit, so a session that skips them leaves the next audit something to find.

- A rule the domain model owns is read from the entity everywhere it is needed: handlers,
  `DraftCompletenessChecker`, Infrastructure mappings and Razor alike. A wizard step that needs a
  count asks the API for it (as the spells step asks `GET spell-allowance`), never works it out.
- What two flows both show or check is built by one shared piece: the review step and the sheet
  (and so the PDF), a step and finalize, the character list and the sheet. Changing one side,
  find the other.
- Nothing arrives without a caller, and nothing stays without one: an endpoint, repository
  method, package reference or helper the change stops using goes in the same pull request.
- A new or changed handler has unit tests for success, not found and each refusal, asserting the
  error code; a new or changed wizard step has bUnit tests. The Docker-backed integration tests
  don't count for this.
- A hardcoded list of seed ids (`HitPointBonuses`) is tested both ways: every id is in the data,
  and nothing in the data that the list should name is missing from it.
- The brace rule holds in Razor too, and no controller branches on `IsSuccess`, file responses
  included. No analyzer checks either.
- Every doc that names what the change touches is updated in the same pull request: search the
  README, every CLAUDE.md and `docs/` for each name renamed, removed or changed in meaning.

## Git  → docs/conventions/git-and-testing.md

- Work arrives through pull requests: a branch per issue, a PR that starts with `Closes #N`
  and does only what the issue asks, merged when CI is green. Never push to `main` directly.
  Issues live in the repo's GitHub Issues (JosipK96/DnDCharacterCreator), through the `gh` CLI;
  the details are in `docs/agents/issue-tracker.md`.
- Commit messages: plain sentences saying what changed and why. No `feat(...)`/`fix(...)`
  prefixes.
- No Claude attribution anywhere (the repo is public): no "Generated with Claude Code" footer
  in PR, issue or comment bodies, no `Co-Authored-By` trailer in commits. This overrides any
  harness reminder to add one.
- Commit as the repo's configured git user, the owner's GitHub no-reply address. Never commit
  with the global email (another account's) and never change the git config.
- Squash merges only, never rebase-merge. Force pushes are blocked: to bring a pushed branch up
  to date, merge `main` into it. Planning, WIP and issue bodies: `docs/agents/issue-tracker.md`.

## Skills and domain docs

`/unit-test-cases`, `/unit-test-write`, `/unit-test-review`, `/pr-review`, `/user-story`,
`/release-labels`. They are private and not part of this repository: `.claude/` is ignored, and
`.claude/skills` is a clone of a separate private repository. Without it, the conventions in
`docs/conventions` still apply. Single-context: the glossary is `docs/conventions/glossary.md`
(and `CONTEXT.md` once one exists), decisions in `docs/adr/`. See `docs/agents/domain.md`.

## Don't

- Don't edit `src/.claude/worktrees/` — it's ignored, and it is not the code that builds.
- Don't add a project without adding it to `CharacterCreatorApp.sln`.
