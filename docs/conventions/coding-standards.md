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
