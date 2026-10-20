# Client

- The client is served by the Api and calls it on its own origin: no CORS, no client-side API
  address. Run the Api to use it (see `src/CharacterCreatorApp.Api/CLAUDE.md`).
- The wizard's step order, numbers and names come from `Client/Pages/CharacterCreation/
  WizardSteps`; a step reads its heading and "Next" label from `WizardStepBase`, never a literal.
- Sheet numbers come from `CharacterStatsCalculator` through the API; don't recompute a modifier
  in a Razor page. What each step may offer follows the D&D rules in
  `src/CharacterCreatorApp.Domain/CLAUDE.md`.
