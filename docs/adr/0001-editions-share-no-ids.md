# The 2014 and 2024 rules editions share no ids

A character is built with one rules edition, 2014 or 2024, chosen in the wizard's first step. The two editions often reuse a name for different rules (the 2024 Fighter, Alert or human are not the 2014 ones), so instead of one row with per-edition variants, each edition gets its own rows: a 2024 entity whose name exists in 2014 gets a suffix on its id (`-2024` for classes, subclasses and features, `_2024` for spells, feats, species and backgrounds). Reference lookups take `?edition=`, and every step handler refuses an entity from the other edition with `CharacterDraftErrors.EditionMismatch`, so a draft can never mix them.

## Consequences

- Never look an entity up by name or strip a suffix to find "the same thing" in the other edition; they are different things.
- Changing a character's edition means rebuilding its choices, not mapping ids across.
- A feat in both editions with the same slug carries `_2014`/`_2024`, so even 2014 ids are not always the bare slug.
