# The seed data keeps book text only for SRD entries

The reference data is generated from 5etools, which carries every book's full text, but the repository is public and only the SRD 5.1 and 5.2 are free to redistribute (CC-BY 4.0, attribution in `NOTICE.md`). So the generator keeps a description only for entries 5etools marks `srd`/`srd52`, using the SRD's names, and lists those ids in `SrdIds.json`; every other spell, feature, feat, background feature and race trait gets its mechanics and a book-and-page reference, and a non-SRD spell keeps no material wording. We kept the non-SRD content's mechanics rather than dropping it, so characters can still use the whole PHB.

## Consequences

- `SeedDataIntegrityTests` fails on book prose outside `SrdIds.json`; never paste book text into a seed file or the generator to make a description read better.
- Raw 5etools data stays outside the repo; only the generated seed JSON is committed.
