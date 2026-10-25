# Coding standards

Most formatting is settled by `.editorconfig` and enforced at build time
(`EnforceCodeStyleInBuild`), so this covers what a tool cannot decide.

## Primary constructors

Use them, and assign the parameters to `private readonly` fields. In a `class`
the parameters compile to mutable private fields, so capturing them in a
readonly field is what keeps the dependency immutable.

```csharp
public class SetCharacterSubclassCommandHandler(IClassRepository classRepository)
{
    private readonly IClassRepository _classRepository = classRepository;

    // use _classRepository, never classRepository
}
```

## Async

- Async methods crossing a boundary take a `CancellationToken` and forward it.
- No `.Result`, `.Wait()` or `.GetAwaiter().GetResult()`.
- This codebase uses the `Async` suffix on repository methods. Keep it —
  consistency beats the argument either way, and renaming sixty methods buys
  nothing.

## Braces and nesting

Always braces after `if`. Prefer an early return over an `else`:

```csharp
// Good
if (draft == null)
{
    return CharacterDraftErrors.NotFoundById(command.DraftId);
}

return Response.Success();
```

## Generic parameters

Meaningful names prefixed with `T` where the semantics are knowable:
`Entity<TId>`, `IQueryHandler<TQuery, TResult>`. A bare `T` is fine only when
the type really could be anything.

## Comments

Comments explain *why*, not *what*. The method name says what.

No commented-out code — git remembers. A commented-out test body sat in this
repo passing vacuously for months.

The comments worth writing here are the ones recording a constraint that is not
visible locally:

```csharp
// ToLower() is what EF translates to SQL lower(). ToLowerInvariant() and the
// StringComparison overloads have no translation, so applying those suggestions
// here would move the comparison out of the database and into memory.
```

## Logging

`ILogger<T>` with message templates and PascalCase parameter names. No string
interpolation — the template *is* the structured event.

```csharp
logger.LogInformation("Races: {Races}, Subraces: {Subraces}", races.Count, subraces.Count);   // good, from RaceSeeder
logger.LogInformation($"Races: {races.Count}, Subraces: {subraces.Count}");                   // bad
```

Handlers do not log. `LoggingBehaviour` logs every request that fails, with its
error codes, and `GlobalExceptionHandler` logs every unhandled exception, so a
handler that returns an error or throws is already covered. Beyond those two, the
seeders and startup log. Several seeders build a message with `string.Join` over
interpolated fragments and pass the result as one parameter — that is allowed,
since the template itself stays constant.

Messages are fragments, not sentences: concise, no trailing period.

## Acronyms

PascalCase initial: `Api`, `Dto`, `Xml`, `Url`, `Phb`. Not `API`, `DTO`.

## Nullability

Nullable reference types are on everywhere. A `!` is a claim you are making —
if the compiler cannot see why a value is non-null, say why in a comment:

```csharp
// IsReadyToFinalize above has already rejected a draft with no level
var abilityScoreError = ValidateBaseAbilityScores(draft.Level!.Value, draft.BaseAbilityScores);
```

Prefer a type that cannot be null over a `!` at every use. `IUserContext.UserId`
is a `Guid`, not a `Guid?`: only handlers behind `[Authorize]` take the context, so
`UserContext` throws if there is no user rather than every handler checking.

## Suppressions

Formatting-only analyzer rules are suppressed centrally in
`analyzers/GlobalSuppressions.cs`, each with a justification. Correctness rules
are not suppressed — fix the code.

A local `[SuppressMessage]` is for a case where the analyzer is right in general
and wrong here. It carries a `Justification` that says what the analyzer cannot
see. "The build failed" is not a justification.

## Before a pull request

The root CLAUDE.md lists these as a checklist. Each came out of the code-quality
audit before 1.0 (#78-#90), which found every one of them in code that had
passed review, because nothing in the build catches them. Here is why each
matters.

**One copy of each rule.** A rule the domain model owns (how many improvements a
level gives, how many languages a background lets the player pick, what a hit
die is) is a member of the entity, and everything else reads it from there:
handlers, `DraftCompletenessChecker`, the read-model mappings in Infrastructure,
and the wizard's Razor steps. The audit found the improvement count written in
three places, a background's language count in an Infrastructure mapping, and
the race's picks worked out again in the race and improvements steps. A copy
works until the rule changes, and then the step offers what finalize refuses.
The wizard asks the API for what a draft owes, as the spells step asks
`GET spell-allowance` and the improvements step `GET fighting-styles`.

**One builder for what two flows share.** The review step's preview and the
saved sheet (and its PDF) show the same thing; a step and finalize check the
same thing; the character list and the sheet offer the same actions. When two
places assemble it separately they drift: the review step once listed feats
differently from the sheet (#52), and armour class had to be added to both
(#53). Share the piece; when a change touches one side, look for the other.

**Nothing without a caller.** An endpoint, repository method, package reference
or helper arrives with the code that uses it, and goes when the last use goes.
Unused code is still read, kept compiling and extended: an endpoint the client
never calls has to repeat every check of the one it does call, or it becomes a
way around them.

**Tests that run without Docker.** A new or changed handler has unit tests for
its success path, its not-found path and each refusal, asserting the error code.
A new or changed wizard step has bUnit tests of what it offers, what it sends
and how it restores a saved draft. The Api integration tests reach most
handlers too, but they need Docker, so they fail late; they are a second check,
not the first.

**Hardcoded ids, guarded both ways.** Some rules can only name seed ids
(`HitPointBonuses`: the hill dwarf, Draconic Resilience, Tough). A test that the
ids still exist is half the guard. The other half is a test that the data holds
nothing the list should name and doesn't: otherwise a regenerated seed file with
a new book's dwarf gives that character too few hit points and every test
passes. Prefer reading the rule from the data, as armour class does.

**Rules no analyzer checks.** The analyzers don't run on Razor, so the brace
rule there is on whoever writes it. A controller turns a `Response` into a
result through `ApiController`'s helpers and never branches on `IsSuccess`
itself, so which status a success or failure gets is decided in one place. The
PDF endpoint still branches, for want of a helper that returns a file; #82 adds
one, and later file endpoints use it.

**Docs in the same pull request.** When a change renames, removes or changes
what something means, search the README, every CLAUDE.md and `docs/` for it and
fix them in the same pull request. `ClaudeMdReferencesTests` catches a
backticked name in a CLAUDE.md that the code no longer declares, but not a
statement that stopped being true.
