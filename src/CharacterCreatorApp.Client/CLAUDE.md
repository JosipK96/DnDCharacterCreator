# Client

- The client is served by the Api and calls it on its own origin: no CORS, no client-side API
  address. Run the Api to use it (see `src/CharacterCreatorApp.Api/CLAUDE.md`).
- The wizard's step order, numbers and names come from
  `Client/Pages/CharacterCreation/WizardSteps`; a step reads its heading and "Next" label from `WizardStepBase`, never a literal.
- Every step shows what the draft holds when it opens, so coming back to it, or editing a saved
  character ("Edit" on the list and the sheet), goes through it unchanged. In an edit
  (`CharacterDraftDetailsApiResponse.CharacterId` set) the progress map (`Components/WizardProgress.razor`) goes to any step and marks
  the ones in `ChoicesNeeded` (`WizardSteps.StepsOf`); saving returns to the same sheet.
- A level-up ("Level up" on the list and the sheet; `LevelUpChoices` set) goes through its own
  flow, `WizardSteps.LevelUpOf`: the Hit Points step (`WizardSteps.HitPoints`, which a new
  character never visits), then only the steps the new level brings, then Review. Every step
  takes the flow (`WizardStepBase.Flow`), so its heading number and "Next" label follow it. The
  class step keeps the class (its ClassLocked parameter), and the improvements step keeps the
  earlier improvements beside a new one.
- Under each list the draft's books filter (race, class, subclass, background, feats, spells),
  `Components/HiddenOptionsNote.razor` says what the unticked books hold, counted by the API
  (`LookupService.GetHiddenOptionsAsync`). Its button calls `WizardStepBase.OnChangeBooks`: the
  wizard goes to step 1 and back to the step after, unless an edition change or an unticked book
  cleared choices. A level-up has no step 1, so its notes have no button.
- Sheet numbers come from `CharacterStatsCalculator` through the API; don't recompute a modifier
  in a Razor page. The sheet and the review step show spell slots and Pact Magic through the
  one shared component, Shared/SpellSlotsPanel.razor. What each step may offer follows the D&D rules in
  `src/CharacterCreatorApp.Domain/CLAUDE.md`.

- Unhandled errors (Blazor logs them) go to the API through `Services/ErrorReporting/ClientErrorLoggerProvider`
  (#77): exception type, stack, route without query and version, never the message, at most five
  per page load. The client holds no error-tracker key.

## Installable app (#128)

- `wwwroot/manifest.webmanifest` (name, `standalone`, theme `ink` and background `paper` as hex,
  the one place a hex value is right) and `index.html`'s `manifest` and `apple-touch-icon` links
  make it installable. Icons are `icon-192.png`, `icon-512.png`, `icon-maskable-512.png`
  and `apple-touch-icon.png`.
- The project file's ServiceWorker item makes `service-worker.js` the development worker (caches
  nothing) and `service-worker.published.js` the published one, with the file list
  `service-worker-assets.js` generated at publish. The published worker caches those files, never
  anything under `/api/`, and does not `skipWaiting` on its own.
- An update: a release changes the list, the browser installs the new worker, which waits.
  `wwwroot/js/app-update.js` tells `Shared/UpdatePrompt.razor` (in MainLayout) through
  its UpdateReady method; its Reload button posts `SKIP_WAITING` and the page reloads onto the new files.
  A new static file type must match a pattern in the worker's `offlineAssetsInclude` to be cached.

## Styling

- The look is a dungeon map on graph paper. `Styles/app.css` is the stylesheet's source: the
  palette (`paper`, `room`, `ink`, `pencil`, `plum`, `red`, `green`, `amber` and their tints)
  and the two font families are named once in its `@theme`, beside the `room`, `hatch` and
  `btn-main`/`btn-secondary`/`btn-danger` utilities. Use those names, never a hex value or a
  Tailwind default colour in new markup. Red is for errors and delete only; Plum marks where
  the player is and what they picked.
- `Tailwind.targets` builds it at build time with Tailwind's standalone CLI (pinned version and
  SHA-256 per platform, downloaded once into `obj/tailwind`) into `wwwroot/css/app.css`, which
  is generated and gitignored: never edit it. The CLI only sees classes written out in full in
  `Pages/`, `Shared/`, `App.razor` and `wwwroot/index.html`, so never build a class name from
  pieces (`$"bg-{colour}"`).
- Barlow and Barlow Condensed are served from `wwwroot/fonts` with their `OFL.txt`; nothing
  loads from Google.
- Below 768px every button, link, select, text box and checkbox label is at least 44 by 44px,
  by one rule in `app.css`'s base layer; `WizardSmokeTests`' phone run measures them on every
  step and the sheet. Don't size a new control under that.
- Every message box is `Shared/Message.razor` (`MessageKind` Error, Success, Warning, Info). Its
  role (`alert` for errors only, `status` for success and warnings, `note` for info) is how
  tests find it, never a colour class. A framed section is `Shared/Room.razor`.
- `Components/WizardProgress.razor` is a map of rooms from 700px wide and a one-line "Room N of M" with squares
  below it; both carry `data-state` (done, current, needs-pick, ahead) for the tests.
- The wizard steps are drawn with the shared pieces in `Shared/`, never with styles of their own.
  An option to pick (a race, class, background, feat) is a `Shared/OptionRoom.razor` on an
  option-grid, with the picked one's `Shared/KeyPanel.razor` of `Shared/KeyFact.razor` rows beside
  it in step-columns. A long list (spells, skills, tools, languages) is a
  `Shared/ChoiceSection.razor` of `Shared/ChoiceRow.razor` rows, its `Shared/ChoiceCount.razor`
  Green once complete and Amber otherwise. A hit die is `Shared/HitDie.razor`, a loading list
  `Shared/Spinner.razor`, a number on the review `Shared/StatBox.razor`, and a step's foot
  `Components/StepNavigation.razor`. Tests find them by data-testid and aria-pressed, never by a class.
- The sheet (`Pages/Characters/CharacterDetails.razor`) is rooms in `sheet-columns` (three wide,
  two on a tablet, one on a phone), its combat numbers `Shared/CombatNumber.razor`, its ability
  scores `score-box` with the modifier in a `modifier-tab`, and its saving throws and skills
  `sheet-row` rows with a `prof-dot` (`data-proficient`). The list is a room per character with
  `field` filters and `pager` page numbers (`aria-current="page"` filled Ink). Both ask before a
  delete with `Shared/ConfirmDelete.razor` (role `dialog`). The PDF is drawn by the API and keeps
  its own look.
- The home, sign-in, register, password and admin pages are one `Shared/Room.razor` each on the
  paper, with a `field` and a visible label per input and a `btn-main` for the action. No Razor
  file uses a Tailwind default colour (`gray-`, `indigo-`, `red-` and so on).
