# Application

The D&D rules a handler checks are in `src/CharacterCreatorApp.Domain/CLAUDE.md`: read it before
changing a character creation step, `CharacterBuilder` or `DraftCompletenessChecker`.

## Structure

- The Application layer is CQRS over MediatR: use `ICommand`/`ICommandHandler` and
  `IQuery`/`IQueryHandler` from `Common/Abstractions`, not raw `IRequest`. One handler per file
  under `Features/<Area>/<UseCase>/`.
- MediatR stays on 12.4.1: 13 and later need a paid or registered license in production, and
  12.5.0 changes the pipeline delegate. Dependabot ignores it; move it only by hand.
- Inside Application, `Features/` holds only use cases and what a single feature area owns.
  Anything two areas share sits beside it: `Persistence/` (`IUnitOfWork` and the repository
  interfaces), `ReadModels/`, `Errors/` (the shared `<Area>Errors`), and `Common/` for plumbing
  that knows nothing about D&D (CQRS abstractions, pipeline behaviours, filtering, `IUserContext`).
- List queries take `FilterOptions<T>` from `Common/Filtering`.
- Shared creation logic lives in `Features/CharacterCreation/Building`: `CharacterBuilder` turns
  a draft into a `Character`, `DraftCompletenessChecker` says what is still missing,
  `DraftSourceLoader` fetches the entities a draft names by id. Finalize and preview both use
  all three — that is the point, so the review step cannot disagree with what finalize builds.
  `CharacterFeatEntries` lists the feats taken, one entry per take with its spells, for the
  review step and the sheet alike. `ToolPicks`, `FeatProficiencyPicks` and `BonusSpells` list the
  picks a draft owes and check them, for their step and finalize alike.
- Application reaches ASP.NET Core Identity through `IIdentityService`.

## Errors and validation  → docs/conventions/errors-and-validation.md

- Every error code is a static method on an `<Area>Errors` class, namespaced
  `"<Area>.<Reason>"`. Never write `Error.Validation("Some.Code", ...)` at a call site.
- Request-shape rules go in a FluentValidation validator, which `ValidationBehaviour`
  (`Application/Common/Behaviours`) runs in the pipeline. Every rule sets `.WithErrorCode(...)`
  from the `Codes` class nested in the matching `<Area>Errors`; without it the code is
  FluentValidation's validator name (`NotEmptyValidator`).
- Anything needing the database is a handler check, not a validator.
