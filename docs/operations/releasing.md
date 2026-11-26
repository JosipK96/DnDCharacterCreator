# Releasing

Merging to `main` deploys nothing. A version goes live when the owner publishes a GitHub Release
`vX.Y.Z` (semantic versioning: major for breaking changes, minor for features, patch for fixes),
and what goes live is exactly that tag's commit. The decision is in
[docs/adr/0004-release-by-tag.md](../adr/0004-release-by-tag.md). Where the app runs is in
[hosting.md](hosting.md).

- **Releasing:** on GitHub, Releases > Draft a new release, create the tag `vX.Y.Z` on the
  `main` commit to ship, and publish. [release.yml](../../.github/workflows/release.yml) then
  checks the tag looks like `vX.Y.Z` and that CI passed on its commit, moves the `production`
  branch to that commit plus a `version.txt`, and adds the pull requests since the previous tag,
  with their labels and Deployment notes, to the release. Render deploys `production`; the
  Actions run's summary names the commit. A `db-migration`, `reference-data`, `config-change` or
  `container-change` label in the notes means the release takes more than a build.
- **Render's side:** the service follows the `production` branch with Auto-Deploy On Commit
  (`branch: production` and `autoDeployTrigger: commit` in `render.yaml`, applied from the
  dashboard). No deploy hook or extra secret is involved.
- **The version** is built into the image (the Dockerfile reads `version.txt`) and shown in the
  footer and at `/health`, which answers `{"status":"Healthy","version":"1.0.0"}`. A build that
  is not a release shows `0.0.0-dev`.
- **A hotfix:** branch `hotfix/vX.Y.Z` from the live tag (`git switch -c hotfix/v1.0.1 v1.0.0`),
  fix it, and open the fix as a pull request to `main` as well, so CI runs on it and `main`
  gets it. Then publish a release `v1.0.1` with its target set to that hotfix branch (the
  tagged commit must be the one CI passed on). Delete the branch after the tag.
- **Rolling back:** open the older release's Actions run and re-run it: that moves `production`
  back to that tag. A database migration is not undone by this; restore from a backup
  ([backup-and-restore.md](backup-and-restore.md)) if it must be.
- **A dry run** is a pre-release: its tag, say `v0.0.0-dryrun.1`, goes through the same workflow
  but moves `production-dryrun`, which Render does not follow. Delete the release, the tag and
  that branch afterwards.
- **Never** edit or merge into `production` by hand, or protect it against force pushes: the
  workflow rewrites it for every release, and asks for `contents: write` itself, so no
  repository setting needs to change.
