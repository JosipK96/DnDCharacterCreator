# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`docs/conventions/glossary.md`**: the project's domain vocabulary (race and subrace, species and lineage, edition, ASI, origin feat and so on). It is the glossary until a `CONTEXT.md` exists.
- **`CONTEXT.md`** at the repo root, if it exists.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill creates them lazily when terms or decisions actually get resolved.

CLAUDE.md and the rest of `docs/conventions/` hold the architecture, D&D rules and coding conventions; read the section that matches the area too.

## File structure

Single-context repo:

```
/
├── CLAUDE.md
├── CONTEXT.md                 (once created)
├── docs/
│   ├── adr/                   (once created)
│   └── conventions/
│       └── glossary.md
└── src/
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in the glossary. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders), but worth reopening because…_
