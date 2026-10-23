using CharacterCreatorApp.Client.Services.Http;

using Microsoft.AspNetCore.Components;

namespace CharacterCreatorApp.Client.Pages.CharacterCreation.Components;

/// <summary>
/// What every wizard step shares: the draft it edits, and the busy flags and error message around
/// each call to the API. A step fetches what it shows with <see cref="LoadAsync"/> and stores the
/// player's choice with <see cref="SaveAsync"/>; both show the spinner, turn a failed call into
/// <see cref="ErrorMessage"/>, and return whether the call worked, so a step moves on only after
/// its save succeeded.
/// </summary>
public abstract class WizardStepBase : ComponentBase
{
    [Parameter]
    public Guid DraftId { get; set; }

    /// <summary>This step's number in <see cref="WizardSteps"/>, passed in by the wizard.</summary>
    [Parameter]
    public int Step { get; set; }

    /// <summary>
    /// The steps this wizard goes through, by number: every one for a new character or an edit,
    /// only what the new level brings for a level-up (<see cref="WizardSteps.LevelUpOf"/>).
    /// </summary>
    [Parameter]
    public IReadOnlyList<int> Flow { get; set; } = WizardSteps.Creation;

    /// <summary>The "Next" button's text, naming the step that follows this one in the flow.</summary>
    protected string NextLabel => $"Next: {WizardSteps.NameOf(NextStep)}";

    private int Position => Flow.ToList().IndexOf(Step);

    private int NextStep => Position >= 0 && Position + 1 < Flow.Count ? Flow[Position + 1] : Step + 1;

    protected bool IsLoading { get; private set; }

    protected bool IsSaving { get; private set; }

    protected string? ErrorMessage { get; set; }

    /// <summary>"Step 6: Ability Scores", numbered by where the step sits in the flow rather than by hand.</summary>
    protected string StepHeading(string title) => $"Step {(Position >= 0 ? Position + 1 : Step)}: {title}";

    protected Task<bool> LoadAsync(Func<Task> load, string whatFailed) =>
        RunAsync(load, busy => IsLoading = busy, whatFailed);

    protected Task<bool> SaveAsync(Func<Task> save, string whatFailed = "Failed to save") =>
        RunAsync(save, busy => IsSaving = busy, whatFailed);

    /// <summary>
    /// The same, for a call with a spinner of its own, such as the subclass list that loads
    /// under the class list without hiding it.
    /// </summary>
    /// <remarks>
    /// Only a failed call is caught. Anything else is a bug in the step, and Blazor's error bar
    /// says so better than a message reading "Failed to save: Object reference not set".
    /// </remarks>
    protected async Task<bool> RunAsync(Func<Task> work, Action<bool> setBusy, string whatFailed)
    {
        setBusy(true);
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            await work();
            return ErrorMessage is null;
        }
        catch (ApiException ex)
        {
            ErrorMessage = $"{whatFailed}: {ex.Message}";
            return false;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = $"{whatFailed}: the server could not be reached.";
            return false;
        }
        finally
        {
            setBusy(false);
            StateHasChanged();
        }
    }
}
