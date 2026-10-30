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
- **No attribution** in anything posted to GitHub: no "Generated with" footers or co-author lines.
