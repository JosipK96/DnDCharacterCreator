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
- Editing a saved character (`StartCharacterEditCommandHandler`) opens a copy of its build as a
  new draft, dropping an edit left unsaved; finalize then replaces the character it names and
  deletes the old build. A level-up (`StartCharacterLevelUpCommandHandler`) opens it the same
  way (`CharacterEditStarter`), then one level up; `SetHitPointsCommandHandler` keeps its hit
  die result, and `SetCharacterClassCommandHandler` refuses another class in one. `ICharacterDraftRepository.GetByIdForUpdateAsync` finds only unfinished
  drafts, so no step, finalize or delete can touch a build; an edit is never the
  `GetIncompleteDraft` a new character resumes.
- A saved character's sheet, on the page and as a PDF, is loaded and worked out by one
  `CharacterDetailsLoader`, so the two cannot disagree. It returns a not-found error when the
  character's class, race, background, subclass or subrace can't be loaded, never a sheet built
  on fallbacks. What the sheet lists (proficiencies, features, spells, languages, feats) is
  resolved by `CharacterSheetContentLoader` and its numbers by `CharacterStatsInput.For`; the
  review step's preview calls the same two, so only the read models differ. The PDF is drawn behind
  `ICharacterSheetPdfRenderer`, so Application references no PDF library.
- Application reaches ASP.NET Core Identity through `IIdentityService`.

## Errors and validation  → docs/conventions/errors-and-validation.md

- Every error code is a static method on an `<Area>Errors` class, namespaced
  `"<Area>.<Reason>"`. Never write `Error.Validation("Some.Code", ...)` at a call site.
- Request-shape rules go in a FluentValidation validator, which `ValidationBehaviour`
  (`Application/Common/Behaviours`) runs in the pipeline. Every rule sets `.WithErrorCode(...)`
  from the `Codes` class nested in the matching `<Area>Errors`; without it the code is
  FluentValidation's validator name (`NotEmptyValidator`).
- Anything needing the database is a handler check, not a validator.
