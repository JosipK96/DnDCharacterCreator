# Client

- The client is served by the Api and calls it on its own origin: no CORS, no client-side API
  address. Run the Api to use it (see `src/CharacterCreatorApp.Api/CLAUDE.md`).
- The wizard's step order, numbers and names come from
  `Client/Pages/CharacterCreation/WizardSteps`; a step reads its heading and "Next" label from `WizardStepBase`, never a literal.
- Every step shows what the draft holds when it opens, so coming back to it, or editing a saved
  character ("Edit" on the list and the sheet), goes through it unchanged. In an edit
  (`CharacterDraftDetailsApiResponse.CharacterId` set) the progress bar goes to any step and marks
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
