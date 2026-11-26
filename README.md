# D&D 5E Character Creator

[![CI](https://github.com/JosipK96/DnDCharacterCreator/actions/workflows/ci.yml/badge.svg)](https://github.com/JosipK96/DnDCharacterCreator/actions/workflows/ci.yml)

A web app for building Dungeons & Dragons 5th Edition characters, step by step, under either
the 2014 or the 2024 rules, and keeping them on a character sheet.

## What it does

- **Both rules editions:** a character picks 2014 or 2024 first, and every later step offers
  only that edition's options.
- **Books to allow:** the table ticks the books it allows, and the wizard offers only what they hold.
- **A guided wizard** from name and level to a review of the finished sheet, saved after every step.
- **Edit and level up** a saved character in the same wizard, keeping every earlier choice.
- **The rules are checked, not just offered:** spell counts, feat prerequisites, duplicate
  skills, and choices an earlier change undid.
- **A computed sheet** (hit points, armour class, saves, skills, features, spells and slots),
  downloadable as a PDF on A4 or Letter.
- **Invite-only sign-up** for a private group, and an installable app on a phone.

## Built with

.NET 10: an ASP.NET Core API and a Blazor WebAssembly client served by it, over PostgreSQL with
EF Core, in a clean architecture with CQRS over MediatR. The client is styled with Tailwind CSS.
The game data is generated from [5etools](https://github.com/5etools-mirror-3/5etools-src) data
by a tool in this repository.

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
docs/development.md                    running the API on its own, the reference data, the tests
docs/conventions/                      coding, error, data and testing conventions
docs/adr/                              decisions that are not obvious from the code
docs/operations/                       the live site: hosting, releasing, backup and restore
docs/agents/                           how issues, branches and pull requests are handled
configurations/Env_files/              non-secret container settings
```

## Getting started

The API needs a PostgreSQL connection string and a JWT signing key. The key is never kept in
`appsettings.json`; the app refuses to start without it.

Copy `.env.example` to `.env` in the repository root and set `JWT_KEY` (at least 32
characters; the file shows how to generate one). Then, from the repository root:

```bash
docker compose up -d --build
```

The app is at http://localhost:8080 and Swagger at http://localhost:8080/swagger. The database
is published on port 5433 so it does not collide with a PostgreSQL installed on 5432.

Running the API from Visual Studio or `dotnet run`, and regenerating the reference data, are in
[docs/development.md](docs/development.md).

## Running the tests

```bash
dotnet test --project tests/CharacterCreatorApp.Domain.UnitTests
```

Each project under `tests/` runs the same way. The integration test projects need a running
Docker daemon. Which test class covers what is in [tests/CLAUDE.md](tests/CLAUDE.md), and the
wizard's browser tests are in [docs/development.md](docs/development.md#tests).

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
