# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body-file <file> --assignee @me`. Assign every issue to the repository owner unless told to leave it unassigned.
- **Bodies always go through a file.** Write the whole issue, PR or comment body to a file and pass `--body-file`, for creates and edits alike. Reading a body into a shell variable and writing it back flattened the line breaks of #24 and the Roadmap #34, and PowerShell 5.1 garbles UTF-8 passed inline.
- **Read an issue**: `gh issue view <number> --comments`, filtering comments by `jq` and also fetching labels.
- **List issues**: `gh issue list --state open --json number,title,body,labels,comments --jq '[.[] | {number, title, body, labels: [.labels[].name], comments: [.comments[].body]}]'` with appropriate `--label` and `--state` filters.
- **Comment on an issue**: `gh issue comment <number> --body-file <file>`
- **Apply / remove labels**: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- **Close**: `gh issue close <number> --comment "..."`

Infer the repo from `git remote -v`; `gh` does this automatically when run inside a clone.

## Issues, branches and pull requests

- **Plan on the issue before building.** For anything bigger than a small fix, post a `## Plan` comment (or edit the body) before the first code change: what the code and data say, the decisions and why, what is out of scope, which tests. Decisions the owner makes in chat go onto the issue too. Chat context is lost between sessions; the issue is the record.
- **Push work in progress.** Commit and push to the issue's branch with a message saying what is left, so a fresh session can resume from the issue and the branch alone.
- **One issue, one branch, one pull request.** Work starts from an issue, on its own branch, and arrives as a pull request whose body begins with `Closes #<number>`, which links the two and closes the issue on merge.
- **A pull request does what its issue asks, and nothing else.** Anything found along the way that the issue does not mention becomes a new issue, not part of the pull request.
- **The exception** is small changes to the repository as a whole (the README, housekeeping like a folder rename): those may be a pull request without an issue.
- **Merging** happens only when CI is green on the current `main`, after `/pr-review` has posted its report on the pull request. Once reviewed, turn on auto-merge (`gh pr merge <number> --auto --squash`): branch protection requires both CI jobs, and GitHub squash-merges the pull request when they pass, closing the issues it names. Nobody needs to watch or poll CI for it.
- **One at a time, in the agreed order.** Strict protection leaves the other open pull requests behind after each merge; bring the next one up to date with `gh pr update-branch <number>`.
- **A stale head** (no CI on the new commit, "checking ability to merge" forever, or merge says out of date while compare shows nothing behind): close and reopen the pull request, which resyncs it and starts CI, then turn auto-merge on again.
- **After the merge**, tick the issue's acceptance boxes (edit the body with `--body-file`), noting any box that was not verified and why.
- **Releases are the owner's.** A merge to `main` deploys nothing. The owner publishes a GitHub Release `vX.Y.Z` on a `main` commit, and `.github/workflows/release.yml` deploys that tag; an issue session never creates a tag or release (a dry run on a pre-release tag is the one exception, with the owner's say-so). A hotfix is a branch `hotfix/vX.Y.Z` from the live tag, fixed there, with the fix also opened as a pull request to `main`, then released with the hotfix branch as the tag's target. Steps: README, Releasing; why: `docs/adr/0004-release-by-tag.md`.
- **No attribution** in anything posted to GitHub: no "Generated with" footers or co-author lines.

## Pull requests as a triage surface

**PRs as a request surface: no.** _(Set to `yes` if this repo treats external PRs as feature requests; `/triage` reads this flag.)_

When set to `yes`, PRs run through the same labels and states as issues, using the `gh pr` equivalents:

- **Read a PR**: `gh pr view <number> --comments` and `gh pr diff <number>` for the diff.
- **List external PRs for triage**: `gh pr list --state open --json number,title,body,labels,author,authorAssociation,comments` then keep only `authorAssociation` of `CONTRIBUTOR`, `FIRST_TIME_CONTRIBUTOR`, or `NONE` (drop `OWNER`/`MEMBER`/`COLLABORATOR`).
- **Comment / label / close**: `gh pr comment`, `gh pr edit --add-label`/`--remove-label`, `gh pr close`.

GitHub shares one number space across issues and PRs, so a bare `#42` may be either: resolve with `gh pr view 42` and fall back to `gh issue view 42`.

## When a skill says "publish to the issue tracker"

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a single issue with **child** issues as tickets.

- **Map**: a single issue labelled `wayfinder:map`, holding the Notes / Decisions-so-far / Fog body. `gh issue create --label wayfinder:map`.
- **Child ticket**: an issue linked to the map as a GitHub sub-issue (`gh api` on the sub-issues endpoint). Where sub-issues aren't enabled, add the child to a task list in the map body and put `Part of #<map>` at the top of the child body. Labels: `wayfinder:<type>` (`research`/`prototype`/`grilling`/`task`). Once claimed, the ticket is assigned to the driving dev.
- **Blocking**: GitHub's **native issue dependencies**, the canonical, UI-visible representation. Add an edge with `gh api --method POST repos/<owner>/<repo>/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`, where `<blocker-db-id>` is the blocker's numeric **database id** (`gh api repos/<owner>/<repo>/issues/<n> --jq .id`, _not_ the `#number` or `node_id`). GitHub reports `issue_dependencies_summary.blocked_by` (open blockers only, the live gate). Where dependencies aren't available, fall back to a `Blocked by: #<n>, #<n>` line at the top of the child body. A ticket is unblocked when every blocker is closed.
- **Frontier query**: list the map's open children (`gh issue list --state open`, scoped to the map's sub-issues / task list), drop any with an open blocker (`issue_dependencies_summary.blocked_by > 0`, or an open issue in the `Blocked by` line) or an assignee; first in map order wins.
- **Claim**: `gh issue edit <n> --add-assignee @me`, the session's first write.
- **Resolve**: `gh issue comment <n> --body-file <answer file>`, then `gh issue close <n>`, then append a context pointer (gist + link) to the map's Decisions-so-far.
