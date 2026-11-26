# Development

What a developer needs beyond the README's Docker quick start: running the API on its own,
the reference data, and the tests. The architecture and conventions are in the root
[CLAUDE.md](../CLAUDE.md), each project's own CLAUDE.md and [docs/conventions](conventions).

## The API from Visual Studio or `dotnet run`

Start only the database from the compose file, and the API against it:

```bash
docker compose up -d postgres
```

`appsettings.Development.json` already points at that database (`localhost:5433`). Put the
signing key in user secrets, from `src/CharacterCreatorApp.Api`:

```bash
dotnet user-secrets set "JwtSettings:Key" "$(openssl rand -base64 48)"
```

Run the `http` or `https` profile of `CharacterCreatorApp.Api`; it opens the client at
http://localhost:5207 (or https://localhost:7274). The compose file is also in the solution as
`docker-compose`, so Visual Studio can start the whole stack instead.

## The client's build

The client is styled with Tailwind CSS, built at build time by Tailwind's standalone CLI, so no
Node install is needed. The first build of the client downloads the pinned CLI (about 110 MB,
checked against its SHA-256) into the client's `obj` folder, so that build needs the network;
later builds reuse it. The theme, the fonts, the build steps and the installable app (the
manifest and the service worker) are described in
[src/CharacterCreatorApp.Client/CLAUDE.md](../src/CharacterCreatorApp.Client/CLAUDE.md).

## Reference data

On startup the API applies pending migrations and loads the reference data from the JSON files
in `src/CharacterCreatorApp.Infrastructure/Data`. When any of those files changed since the
last start, the reference tables are replaced with what the files say; users and their
characters stay. A change that removes a race, class or background a character uses stops
startup instead, and changes nothing.

Every file but `Proficiencies.json` is written by the seed data generator, from a copy of the
5etools data kept outside this repository:

```bash
dotnet run --project tools/CharacterCreatorApp.SeedDataGenerator -- <path to the 5etools data>
```

It prints which ids each file gained and lost. A lost race, subrace, class, subclass or
background that a character uses would stop the next startup, so check that list before
committing. After regenerating, run the seed data checks; they need no database:

```bash
dotnet test --project tests/CharacterCreatorApp.Infrastructure.IntegrationTests --filter-class "*.SeedDataIntegrityTests" "*.ClassSeedDataTests" "*.RaceSeedDataTests" "*.BackgroundSeedDataTests" "*.SpellSeedDataTests" "*.FeatSeedDataTests"
```

What the generator keeps and why (the SRD-only rule) is in
[tools/CharacterCreatorApp.SeedDataGenerator/CLAUDE.md](../tools/CharacterCreatorApp.SeedDataGenerator/CLAUDE.md).

## The local database

The compose database and the Testcontainers databases run PostgreSQL 18, the major version the
live database runs ([operations/hosting.md](operations/hosting.md)). A volume created by an older
compose file holds PostgreSQL 16 data, which 18 cannot open: the container exits at start.
Delete it (local users and characters go with it; the reference data reloads on the next start),
or `pg_dump` it first from the old image and restore into the new one.

To start from nothing, users and characters included, delete the compose database volume:

```bash
docker compose down -v
```

## Tests

The tests run on Microsoft.Testing.Platform (set in `global.json`), so a project is passed with
`--project` and one class picked with `--filter-class "*.ClassName"`:

```bash
dotnet test --project tests/CharacterCreatorApp.Domain.UnitTests --filter-class "*.CharacterDraftTests"
```

Each project under `tests/` runs on its own the same way. The unit test projects need nothing
else; the integration test projects start PostgreSQL through Testcontainers, so they need a
running Docker daemon. Which class covers which area is in [tests/CLAUDE.md](../tests/CLAUDE.md).

CI (`.github/workflows/ci.yml`) runs all of them on every pull request, with the build's
warnings treated as errors, test coverage with a floor for Domain and Application (see
[conventions/git-and-testing.md](conventions/git-and-testing.md)), a vulnerable-package check,
and a build of the Docker image. A separate CodeQL workflow scans the C# code for security flaws
on every pull request and weekly, and shows its alerts under the repository's Security tab.

### The wizard's tests

- `tests/CharacterCreatorApp.Client.UnitTests`: bUnit tests of the steps with rules of their own
  (point buy, the dice roll, improvements and Fighting Styles, spell counts, the books and the
  notes on what the unticked ones hold), steps filled in from the draft, the marks of an edit, a
  level-up's flow and hit points, and the sheet's Download PDF. They render the components in
  memory over a fake API, so they need neither Docker nor a browser.

  ```bash
  dotnet test --project tests/CharacterCreatorApp.Client.UnitTests
  ```

- `tests/CharacterCreatorApp.Client.IntegrationTests`: a Playwright smoke test that registers
  with an invite code, builds a 2014 and a 2024 character, edits one, levels one up twice,
  downloads a sheet PDF and sees a refused pick, all through the real wizard in a headless
  Chromium, against the Api on a local port and a Testcontainers PostgreSQL. It needs Docker.
  The first run downloads Chromium (about 150 MB, into the user's Playwright cache). To do that
  up front, build the project and run its install script (with Windows PowerShell instead of
  PowerShell 7, `powershell -ExecutionPolicy Bypass -File` in place of `pwsh`):

  ```bash
  dotnet build tests/CharacterCreatorApp.Client.IntegrationTests
  ```

  ```bash
  pwsh tests/CharacterCreatorApp.Client.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
  ```

  ```bash
  dotnet test --project tests/CharacterCreatorApp.Client.IntegrationTests
  ```

  A failed run attaches a screenshot of where the wizard stopped to the test results.
