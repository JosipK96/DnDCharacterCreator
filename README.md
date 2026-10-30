# D&D 5E Character Creator

[![CI](https://github.com/JosipK96/DnDCharacterCreator/actions/workflows/ci.yml/badge.svg)](https://github.com/JosipK96/DnDCharacterCreator/actions/workflows/ci.yml)

A web app for building Dungeons & Dragons 5th Edition characters, step by step, under either
the 2014 or the 2024 rules, and keeping them on a character sheet.

## What it does

- **Both rules editions.** A character picks 2014 or 2024 in its first step, and every later
  step offers only that edition's classes, species and races, backgrounds, feats, spells and
  languages.
- **A guided wizard.** Name and level, race, class, background, languages, ability scores,
  proficiencies, spells and improvements, then a review of the finished sheet before saving.
  A draft is saved after every step, so it can be left and picked up again.
- **Saved characters can be edited.** "Edit" opens a character in the same wizard with every
  choice it was made with; saving replaces it and keeps its id, and leaving the edit unsaved
  changes nothing.
- **Saved characters level up.** "Level Up" raises a character by one level and asks only what
  the new level brings, from the class data: its hit points (the hit die rolled, in the app or
  with real dice, or its fixed value), and a subclass, an improvement or feat, new spells or a
  Fighting Style where the level gives one. Every earlier choice stays, and saving keeps the id.
- **The rules are checked, not just offered.** Spell counts and levels come from the class's
  spell table, feat prerequisites are checked against the character, a skill already given by
  the race or background cannot be picked again, and changing an earlier step clears the
  choices that depended on it. The review step and saving use the same checks, so the preview
  never promises a character that saving would refuse.
- **A computed sheet.** Hit points, armour class, saving throws, skills, features, spells and
  spell slots (a warlock's Pact Magic apart) are worked out from the choices, not stored, so they
  stay right when the rules data changes.
- **A character sheet PDF.** The sheet's "Download PDF" button gives the computed sheet as a PDF,
  on A4 or Letter.
- **Sign-up with an invite code.** Registering asks for a code the host gives the group, so the
  address alone is not enough to make an account. Sign-in and registration are rate limited, and
  an account is locked after repeated wrong passwords.
- **Nightly backups.** A workflow keeps an encrypted dump of the database for 30 days (see
  Hosting).

## Built with

.NET 10: an ASP.NET Core API and a Blazor WebAssembly client served by it, over PostgreSQL with
EF Core. The code follows clean architecture with CQRS over MediatR, FluentValidation, and
ASP.NET Core Identity with JWT. The sheet PDF is drawn with QuestPDF. Tests use xUnit v3, Moq, FluentAssertions, bUnit,
Playwright and Testcontainers.

The game data (classes, subclasses, races, backgrounds, feats, spells and languages for both
editions) is generated from [5etools](https://github.com/5etools-mirror-3/5etools-src) data by a
tool in this repository.

## Repository layout

```
src/
  CharacterCreatorApp.Domain           entities and the rules they enforce
  CharacterCreatorApp.Application      use cases (commands and queries), validation
  CharacterCreatorApp.Infrastructure   EF Core, PostgreSQL, Identity, seeding, reference data JSON
  CharacterCreatorApp.Api              HTTP API; also serves the client
  CharacterCreatorApp.Contracts        request and response types shared with the client
  CharacterCreatorApp.Client           Blazor WebAssembly front end
tests/                                 unit and integration test projects: one per layer, two for the client
tools/CharacterCreatorApp.SeedDataGenerator   turns 5etools data into the reference data JSON
docs/conventions/                      coding, error, data and testing conventions
docs/adr/                              decisions that are not obvious from the code
docs/operations/                       running the live site: backup and restore
docs/agents/                           how issues, branches and pull requests are handled
configurations/Env_files/              non-secret container settings
```

## Getting started

The API needs a PostgreSQL connection string and a JWT signing key. The key is never kept in
`appsettings.json`; the app refuses to start without it.

### Everything in Docker

Copy `.env.example` to `.env` in the repository root and set `JWT_KEY` (at least 32
characters; the file shows how to generate one). Then, from the repository root:

```bash
docker compose up -d --build
```

The app is at http://localhost:8080 and Swagger at http://localhost:8080/swagger. The database
is published on port 5433 so it does not collide with a PostgreSQL installed on 5432.

### The API from Visual Studio or `dotnet run`

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

### Reference data

On startup the API applies pending migrations and loads the reference data from the JSON files
in `src/CharacterCreatorApp.Infrastructure/Data`. When any of those files changed since the
last start, the reference tables are replaced with what the files say; users and their
characters stay. A change that removes a race, class or background a character uses stops
startup instead, and changes nothing.

The compose database and the Testcontainers databases run PostgreSQL 18, the major version Neon runs
(see Hosting). A volume created by an older compose file holds PostgreSQL 16 data, which 18 cannot
open: the container exits at start. Delete it (local users and characters go with it; the reference
data reloads on the next start), or `pg_dump` it first from the old image and restore into the new one.

To start from nothing, users and characters included, delete the compose database volume:

```bash
docker compose down -v
```

Every file but `Proficiencies.json` is written by the generator, from a copy of the 5etools
data kept outside this repository:

```bash
dotnet run --project tools/CharacterCreatorApp.SeedDataGenerator -- <path to the 5etools data>
```

It prints which ids each file gained and lost. A lost race, subrace, class, subclass or
background that a character uses would stop the next startup, so check that list before
committing. After regenerating, run the seed data checks; they need no database:

```bash
dotnet test --project tests/CharacterCreatorApp.Infrastructure.IntegrationTests --filter-class "*.SeedDataIntegrityTests" "*.ClassSeedDataTests" "*.RaceSeedDataTests" "*.BackgroundSeedDataTests" "*.SpellSeedDataTests" "*.FeatSeedDataTests"
```

## Running the tests

```bash
dotnet test --project tests/CharacterCreatorApp.Domain.UnitTests
```

The tests run on Microsoft.Testing.Platform (set in `global.json`), so a project is passed with
`--project` and one class picked with `--filter-class "*.ClassName"`.

Each project under `tests/` runs on its own the same way. The unit test projects need nothing
else; the integration test projects start PostgreSQL through Testcontainers, so they need a
running Docker daemon. CI runs all of them on every pull request, with the build's warnings
treated as errors, test coverage with a floor for Domain and Application (see
docs/conventions/git-and-testing.md), a vulnerable-package check, and a build of the Docker image.
A separate CodeQL workflow scans the C# code for security flaws on every pull request and weekly,
and shows its alerts under the repository's Security tab.

The wizard has two test projects of its own:

- `tests/CharacterCreatorApp.Client.UnitTests`: bUnit tests of the steps with rules of their own
  (point buy, the dice roll, improvements and Fighting Styles, spell counts, the books), steps
  filled in from the draft, the marks of an edit, a level-up's flow and hit points, and the
  sheet's Download PDF. They render the components in memory over a fake API, so they need
  neither Docker nor a browser.

  ```bash
  dotnet test --project tests/CharacterCreatorApp.Client.UnitTests
  ```

- `tests/CharacterCreatorApp.Client.IntegrationTests`: a Playwright smoke test that registers
  with an invite code, builds a 2014 and a 2024 character, edits one, levels one up twice,
  downloads a sheet PDF and sees a refused pick, all through the real wizard in a headless
  Chromium, against the Api on a local port and a Testcontainers PostgreSQL. It needs Docker. The first run downloads Chromium (about 150 MB, into the user's Playwright cache);
  to do that up front, build the project and run its install script (with Windows PowerShell
  instead of PowerShell 7, `powershell -ExecutionPolicy Bypass -File` in place of `pwsh`):

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

## Deploying

The image built from `src/CharacterCreatorApp.Api/Dockerfile` is the whole app. A host needs
two settings as environment variables:

- `ConnectionStrings__DefaultConnection`, the host's PostgreSQL connection string
- `JwtSettings__Key`, the signing key

and one it should have before the link is shared:

- `Registration__InviteCode`, the code registering asks for. Give it to the group by hand; it
  is never in the repository. Without it anyone with the address can make an account. Make it
  long and random (a password manager's generator will do): registration is rate limited
  (see Hosting), but the code is still what stops someone guessing.

`configurations/Env_files/api.env` holds the non-secret container settings. To run the compose
file without its Development override, use `docker compose -f docker-compose.yml up -d`.

## Hosting

The group's app runs on [Render](https://render.com)'s free web service, with the database on
[Neon](https://neon.tech)'s free Postgres, both in Frankfurt (decided in #31). The address is
Render's `*.onrender.com` one, over Render's HTTPS.

- **The service** is described by [render.yaml](render.yaml), a Render Blueprint: the Docker
  build, the region, the `/health` check, port 8080, and the non-secret settings from
  `configurations/Env_files/api.env`. It was created in the Render dashboard with New >
  Blueprint, and changes to the file are applied from there.
- **Deploys:** a push to `main` deploys once CI on that commit has passed. Migrations and the
  reference data load run at startup; the deploy log shows "Applying pending migrations" and
  "Seeding reference data". If either fails, the new version never passes the health check and
  the previous one keeps serving.
- **Secrets** live only in the Render dashboard (the service's Environment page), never here:
  - `ConnectionStrings__DefaultConnection`: Neon's *direct* endpoint (not the `-pooler` one, so
    migrations get a normal session), in Npgsql's form rather than the `postgres://` URL Neon
    shows: `Host=<ep-…>.eu-central-1.aws.neon.tech;Database=<db>;Username=<user>;Password=<password>;SSL Mode=VerifyFull;GSS Encryption Mode=Disable`
  - `JwtSettings__Key`: a random key of at least 32 characters
  - `Registration__InviteCode`: the invite code (see Deploying)
- **Sign-in protection** (#72), all optional, each with a default. Environment variables on the
  service, like the secrets, but not secret:
  - `Identity__Lockout__MaxFailedAccessAttempts` (default 5) and
    `Identity__Lockout__DefaultLockoutTimeSpan` (default `00:15:00`): wrong passwords before an
    account is locked, and for how long. A locked account is refused even with the right
    password, with the same "Wrong email or password" answer as any failed sign-in.
  - `RateLimiting__PermitLimit` (default 10) and `RateLimiting__WindowSeconds` (default 60):
    sign-in and registration attempts one client address may make per window, counted together
    and including wrong invite codes; past it the API answers 429. The address is the one
    `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (already set in `render.yaml`) takes from Render's
    `X-Forwarded-For`: only the last entry, which Render's proxy appended, so a header a caller
    adds is ignored. That setting trusts the connection it comes in on, which is right because
    the container is reachable only through Render; behind a host that exposes it directly,
    a caller could forge the address.
  - Passwords need at least 8 characters, an uppercase letter and a digit. Accounts made before
    keep working with their old password.
- **Database version:** Neon runs PostgreSQL 18. `docker-compose.yml` and the three Testcontainers
  fixtures use `postgres:18-alpine` so CI and local runs see what production sees; when Neon
  upgrades, move them (and the `postgres:18` in the backup workflow) in the same change.
- **Backups:** Neon's free plan keeps only 6 hours of history, so a nightly workflow keeps an
  encrypted `pg_dump` for 30 days, and a watch workflow fails when no backup is newer than 48
  hours or the schedule was switched off. Its secrets, and how to restore a dump, are in
  [docs/operations/backup-and-restore.md](docs/operations/backup-and-restore.md).
- **Sleeping:** the free service sleeps after 15 minutes without traffic and takes about a
  minute to wake; Neon's free database suspends after 5 minutes and wakes in under a second.
  To stop the sleeping, change the service's instance type to Starter ($7/month) on its
  Settings page, and `plan: free` in `render.yaml` to `plan: starter`. Nothing else changes.

## Contributing

Work is tracked in GitHub issues; each change goes on its own branch and arrives through a pull
request that CI has passed; branch protection on `main` requires it. A reviewed pull request
with auto-merge turned on merges itself when CI passes. [CLAUDE.md](CLAUDE.md) and [docs/conventions](docs/conventions)
describe the architecture and conventions.

## Legal

The reference data carries the rules text of the System Reference Document (SRD 5.1 and 5.2,
CC-BY 4.0) only; for everything else it gives the game mechanics and the book that has the full
text. See [NOTICE.md](NOTICE.md) for the attribution.

This is unofficial Fan Content permitted under the
[Fan Content Policy](https://company.wizards.com/en/legal/fancontentpolicy). Not approved or
endorsed by Wizards. Portions of the materials used are property of Wizards of the Coast.
©Wizards of the Coast LLC.
