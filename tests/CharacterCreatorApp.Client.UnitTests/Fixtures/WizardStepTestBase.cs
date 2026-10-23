using AngleSharp.Dom;

using Bunit;

using CharacterCreatorApp.Client.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CharacterCreatorApp.Client.UnitTests.Fixtures;

/// <summary>
/// A bUnit context with the client's real services over a <see cref="FakeApi"/>, and the draft the
/// step under test edits. JS interop is strict: a call no test set up fails the test.
/// </summary>
public abstract class WizardStepTestBase : BunitContext
{
    protected WizardStepTestBase()
    {
        Services.AddSingleton(new CharacterDraftService(Api.CreateClient()));
        Services.AddSingleton(new LookupService(Api.CreateClient()));
    }

    protected static Guid DraftId { get; } = Guid.Parse("0b6f3a52-7d2e-4c1a-9a51-3f0c2d7e8b14");

    protected FakeApi Api { get; } = new();

    /// <summary>"/api/character-drafts/{id}" with the step's path after it, as the services build it.</summary>
    protected static string DraftPath(string? step = null) =>
        step is null ? $"/api/character-drafts/{DraftId}" : $"/api/character-drafts/{DraftId}/{step}";

    /// <summary>A step's save as <see cref="FakeApi.Requests"/> lists it: "PUT /api/character-drafts/{id}/{step}".</summary>
    protected static string SaveRoute(string step) => $"PUT {DraftPath(step)}";

    /// <summary>The first button whose text holds the given text.</summary>
    protected static IElement Button<TComponent>(IRenderedComponent<TComponent> cut, string text)
        where TComponent : IComponent =>
        Buttons(cut, text).First();

    protected static List<IElement> Buttons<TComponent>(IRenderedComponent<TComponent> cut, string text)
        where TComponent : IComponent =>
        cut.FindAll("button").Where(b => b.TextContent.Contains(text, StringComparison.Ordinal)).ToList();
}
