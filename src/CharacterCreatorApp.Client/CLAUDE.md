# Client

- The client is served by the Api and calls it on its own origin: no CORS, no client-side API
  address. Run the Api to use it (see `src/CharacterCreatorApp.Api/CLAUDE.md`).
- The wizard's step order, numbers and names come from
  `Client/Pages/CharacterCreation/WizardSteps`; a step reads its heading and "Next" label from `WizardStepBase`, never a literal.
- Every step shows what the draft holds when it opens, so coming back to it, or editing a saved
  character ("Edit" on the list and the sheet), goes through it unchanged. In an edit
  (`CharacterDraftDetailsApiResponse.CharacterId` set) the progress map (`Components/WizardProgress.razor`) goes to any step and marks
  the ones in `ChoicesNeeded` (`WizardSteps.StepsOf`); saving returns to the same sheet.
- A level-up ("Level Up" on the list and the sheet; `LevelUpChoices` set) goes through its own
  flow, `WizardSteps.LevelUpOf`: the Hit Points step (`WizardSteps.HitPoints`, which a new
  character never visits), then only the steps the new level brings, then Review. Every step
  takes the flow (`WizardStepBase.Flow`), so its heading number and "Next" label follow it. The
  class step keeps the class (its ClassLocked parameter), and the improvements step keeps the
  earlier improvements beside a new one.
- Sheet numbers come from `CharacterStatsCalculator` through the API; don't recompute a modifier
  in a Razor page. The sheet and the review step show spell slots and Pact Magic through the
  one shared component, Shared/SpellSlotsPanel.razor. What each step may offer follows the D&D rules in
  `src/CharacterCreatorApp.Domain/CLAUDE.md`.

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
- Every message box is `Shared/Message.razor` (`MessageKind` Error, Success, Warning, Info). Its
  role (`alert` for errors only, `status` for success and warnings, `note` for info) is how
  tests find it, never a colour class. A framed section is `Shared/Room.razor`.
- `Components/WizardProgress.razor` is a map of rooms from 700px wide and a one-line "Room N of M" with squares
  below it; both carry `data-state` (done, current, needs-pick, ahead) for the tests.
- The steps, the sheet, the list and the account pages still carry Tailwind's default dark
  classes inside their panels until their own restyle issues; a page itself sits on the paper.
