# A release is a tag, and publishing it deploys that tag

Until 1.0 every merge to `main` deployed to Render once CI passed (#31, #42). Nothing recorded which commit was live or what a deploy changed beyond the pull request list, the app showed no version, and a merge the group should not see yet could not be told apart from one they should. The owner decided on 2026-10-25 (#70) to deploy a version on purpose.

## Decision

- `main` stays the integration branch. Merging deploys nothing.
- A release is a tag `vX.Y.Z` on a `main` commit and a GitHub Release published for it, with semantic versioning (major for breaking changes, minor for features, patch for fixes). The first is `v1.0.0`.
- Publishing the release runs `.github/workflows/release.yml`, which checks that CI passed on the tagged commit, then force-pushes the `production` branch to a commit holding exactly the tagged tree plus a `version.txt` with the version. Render follows `production` with auto-deploy on commit, so what deploys is the tagged commit, not whatever `main` is by then. The Docker build reads `version.txt` into the assemblies, so the client footer and `/health` show it; any other build shows `0.0.0-dev`.
- The workflow also writes the release notes: the pull requests since the previous tag, each with its labels and "Deployment notes" section.
- A hotfix is a branch `hotfix/vX.Y.Z` from the live tag. Its fix is also a pull request to `main`, and the patch release is tagged on the hotfix branch's commit.

## Why not the alternatives

- **Long-lived `release/vX.Y.Z` branches**: one deployed instance and one developer, so a branch per release would only add cherry-picking.
- **A deploy hook called by the workflow**: Render's hook URL is a secret to keep in GitHub, and it deploys a commit rather than moving a branch, so a rollback or a re-run is a second path to maintain. A branch needs no secret, and re-running an older release's workflow is the rollback.
- **Render's `checksPass` trigger on `main`**: it can only deploy the newest commit.

## Consequences

- `production` is rewritten by the workflow and is never edited or merged by hand. It must not get branch protection that blocks force pushes.
- The deployed tree is the tag plus one file, so `git diff vX.Y.Z origin/production` shows only `version.txt`.
- A tag whose commit CI did not pass cannot be released; a hotfix reaches CI through its pull request to `main`, which tests the same commit.
- Owner-only setup in Render is in the README's Releasing section.
