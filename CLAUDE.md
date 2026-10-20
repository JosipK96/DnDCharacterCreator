# CLAUDE.md

.NET 10 D&D 5E character creator: an ASP.NET Core API and a Blazor WebAssembly client over
PostgreSQL, one solution, clean architecture. Setup and how to run it: see README.md.

## Where the rest is

Each project has its own CLAUDE.md, loaded when you read a file in it. Sessions start in `src/`,
so the two outside it never load on their own: read them before working there.

- `src/CharacterCreatorApp.Domain/CLAUDE.md`: the D&D rules (editions, books, races, classes,
  backgrounds, feats, spells, languages, tools, hit points) and the domain model.
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
- `TreatWarningsAsErrors` is on, with StyleCop, AsyncFixer and `EnforceCodeStyleInBuild`. A
  change is not done until it builds clean. Don't suppress to get green: `analyzers/
  GlobalSuppressions.cs` holds formatting-only rules and a few named domain-vocabulary
  collisions, and anything you add there needs a justification that is not "the build failed".
- Package versions go in `Directory.Packages.props` only; shared build settings in
  `Directory.Build.props` (and `tests/Directory.Build.props`, which inherits and relaxes it).
  Never put a `Version=` on a `PackageReference`.
- CI (`.github/workflows/ci.yml`) runs on every pull request and push to `main`: restore,
  build (warnings as errors), unit tests, integration tests and a vulnerable-package check as
  separate steps, and the Api's Docker image (skipped when a PR changes only Markdown or
  `docs/`). Branch protection requires both jobs before a PR can merge: once reviewed, turn on
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

- Primary constructors preferred; assign parameters to `private readonly _fields` and use the
  field.
- Always braces after `if`. Prefer early return over `else` nesting.
- Async methods crossing a boundary take and forward a `CancellationToken`. No `.Result`/
  `.Wait()`. Existing code uses the `Async` suffix — keep it, don't start a rename.
- Comments explain *why*. No commented-out code: a commented-out test body passed vacuously for
  a long time before anyone noticed.

## Git  → docs/conventions/git-and-testing.md

- Work arrives through pull requests: a branch per issue, a PR that starts with `Closes #N`
  and does only what the issue asks, merged when CI is green. Never push to `main` directly.
  Issues live in the repo's GitHub Issues (JosipK96/DnDCharacterCreator), through the `gh` CLI;
  the details are in `docs/agents/issue-tracker.md`.
- Commit messages: plain sentences saying what changed and why. No `feat(...)`/`fix(...)`
  prefixes.

## Skills and domain docs

`/unit-test-cases`, `/unit-test-write`, `/unit-test-review`, `/pr-review`, `/user-story`,
`/release-labels`. They are private and not part of this repository: `.claude/` is ignored, and
`.claude/skills` is a clone of a separate private repository. Without it, the conventions in
`docs/conventions` still apply. Single-context: the glossary is `docs/conventions/glossary.md`
(and `CONTEXT.md` once one exists), decisions in `docs/adr/`. See `docs/agents/domain.md`.

## Don't

- Don't edit `src/.claude/worktrees/` — it's ignored, and it is not the code that builds.
- Don't add a project without adding it to `CharacterCreatorApp.sln`.
