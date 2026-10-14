# Errors and validation

## Where an error code lives

Every error code is defined once, in an `<Area>Errors` class, as a static method returning
`Error.Validation/NotFound/Conflict/Unauthorized`. Handlers return those methods; nothing
constructs an `Error` with a literal code string inline.

Codes are namespaced `"<Area>.<Reason>"`:

```csharp
public static Error InvalidLanguageCount(int required, int selected) =>
    Error.Validation("CharacterDraft.InvalidLanguageCount",
        $"Must choose exactly {required} language(s). You selected {selected}.");
```

Where the class lives depends on who uses it:

| Class | Lives in | Used by |
| --- | --- | --- |
| `CharacterDraftErrors` | `Features/CharacterCreation` | the creation slices |
| `UserErrors` | `Features/Authentication` | login and register |
| `CharacterErrors` | `Application/Errors` | creation and management both |
| `RaceErrors`, `ClassErrors`, `SpellErrors`, … | `Application/Errors` | lookups and creation |

A class one feature area owns sits in that feature's folder. One that two areas share goes in
`Application/Errors`.

## Validators

Request-shape rules belong in a FluentValidation validator, not in the handler.
`ValidationBehaviour` (`Application/Common/Behaviours`) runs it in the MediatR pipeline and turns
each failure into `Error.Validation`, with the rule's code as `Code` and the property as
`FieldName`.

Every rule sets its code explicitly with `.WithErrorCode(...)`, taken from the `Codes` class
nested in the same `<Area>Errors` class. Without it FluentValidation falls back to the validator's
own name, so the error reads `"NotEmptyValidator"` instead of `"CharacterDraft.NameRequired"`.

```csharp
RuleFor(x => x.Name)
    .NotEmpty()
    .WithErrorCode(CharacterDraftErrors.Codes.NameRequired);
```

Anything that needs the database — does this race grant two skills, is this feat's prerequisite
met — is a handler check returning an `<Area>Errors` method, because a validator has no
repositories.

### What the client receives

Controllers never branch on `IsSuccess` themselves; they return `OkOrProblem(response)`,
`OkOrProblem(response, map)` or `NoContentOrProblem(response)` from `ApiController`. Every
failure, including a 500 from `GlobalExceptionHandler`, has the same body:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Level must be between 1 and 20",
  "instance": "/api/character-drafts/…/name-and-level",
  "errors": [
    { "code": "CharacterDraft.InvalidLevel", "propertyName": "Level", "detail": "Level must be between 1 and 20" }
  ],
  "traceId": "…"
}
```

- The first error's type picks the status (validation 400, not found 404, conflict 409,
  unauthorized 401) and its description is `detail`.
- Every error keeps its own `code`. `propertyName` is set only when a validator rule named a
  field.
- A request MVC could not bind at all (malformed JSON) never reaches a handler; its entries
  have the code `Request.Invalid`.
- A 500 has the code `Server.Unexpected`. Its `detail` is the exception message only in
  Development, because messages can name tables and ids.

A client should show `detail` and branch on `code`. It should never parse the message.

## Which layer decides

- **Validator**: the request is well-formed at all (ids present, list not null, level in 1-20).
- **Handler**: the request makes sense for this draft (counts match what the class grants,
  the referenced ids exist, prerequisites are met).
- **Domain**: the invariants of the entity itself (`IsReadyToFinalize`, ability score caps).

## 5E rules live on the entity

The rules that say *when* a character gets something are members of the entity it comes from:

- `Class.SubclassLevel` — in the 2014 rules clerics, sorcerers and warlocks choose at 1, druids
  and wizards at 2, everyone else at 3.
- `Class.AsiLevels` — fighters and rogues get extra ability score improvements.
- `Class.SavingThrowAbilityIds` — the two saves a class is proficient in.
- `Race.SkillChoiceCount` and `Race.SkillChoiceOptions` (`Race.OffersSkill`), and
  `Race.LanguageChoiceCount`, which a subrace can add to (`Race.LanguageChoiceCountWith`).
- `Background.AbilityScoreOptions` (`Background.AllowsAbilityIncreases`) — the three abilities a
  2024 background raises, +2 and +1 or +1 to each; none for a 2014 one.

They are columns, read from the 5etools data by the seed data generator along with each class's
spell table; `ClassSeedDataTests`, `RaceSeedDataTests` and `BackgroundSeedDataTests` check them
against the book. Never hardcode one of these at a call site. Two bugs came from doing exactly that: the
finalize handler and the subclass handler both assumed every class picks a subclass at level 3.
