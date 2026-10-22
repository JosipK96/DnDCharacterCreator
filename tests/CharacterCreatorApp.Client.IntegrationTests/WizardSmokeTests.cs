using System.Text.Json;
using System.Text.RegularExpressions;

using CharacterCreatorApp.Client.IntegrationTests.Fixtures;
using CharacterCreatorApp.Client.Pages.CharacterCreation;

using Microsoft.Playwright;

namespace CharacterCreatorApp.Client.IntegrationTests;

/// <summary>
/// A player's way through the real wizard: register with the invite code, sign in, and build a
/// character of each rules edition step by step, against the Api and its seeded database. The
/// steps' own rules are in the client's bUnit tests; this checks the pieces work together.
/// </summary>
public partial class WizardSmokeTests(WizardHost host) : IClassFixture<WizardHost>, IAsyncLifetime
{
    private const string Password = "Mellon123";

    private readonly WizardHost _host = host;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public async ValueTask InitializeAsync()
    {
        _context = await _host.Browser.NewContextAsync(new BrowserNewContextOptions { BaseURL = _host.Address });

        // A WebAssembly app's first load downloads the runtime, so the first step gets longer
        _context.SetDefaultTimeout(30_000);
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        // A failed run keeps a picture of where the wizard stopped, beside the test results
        if (TestContext.Current.TestState?.Result == TestResult.Failed)
        {
            TestContext.Current.AddAttachment(
                $"{TestContext.Current.Test?.TestDisplayName}.png",
                await _page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true }),
                "image/png");
        }

        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Wizard_ForA2014Fighter_FinishesTheCharacterAndListsIt()
    {
        // Arrange
        await RegisterAndSignInAsync("wizard-2014");

        // Act: a half-orc fighter, with skills, the Soldier's tool and a Fighting Style to pick
        await StartCharacterAsync("Grom", "2014 rules");
        await PickAsync("Half-Orc");
        await NextAsync("Class");
        await PickAsync("Fighter");
        await NextAsync("Background");
        await PickAsync("Soldier");
        await NextAsync("Languages");
        await NextAsync("Ability Scores");
        await StandardArrayAsync();
        await NextAsync("Proficiencies");
        await PickFirstAsync(2);
        await PickToolAsync();
        await NextAsync("Improvements");
        await _page.GetByRole(AriaRole.Combobox).SelectOptionAsync(new SelectOptionValue { Label = "Defense" });
        await NextAsync("Spells");
        await Expect(_page.GetByText("No Spells Available")).ToBeVisibleAsync();
        await NextAsync("Review");
        await CreateAsync("Grom");

        // Assert
        await _page.GotoAsync("/characters");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Wizard_ForA2024Wizard_FinishesTheCharacterAndListsIt()
    {
        // Arrange
        await RegisterAndSignInAsync("wizard-2024");

        // Act: a human wizard, with a size, a skill and an Origin feat from the species, the
        // background's ability increases, languages, cantrips and spells to pick
        await StartCharacterAsync("Elara", "2024 rules");
        await PickAsync("Human");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Medium", Exact = true }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Next: Choose Skills" }).ClickAsync();
        await PickFirstAsync(1);
        await NextAsync("Class");
        await PickAsync("Wizard");
        await NextAsync("Background");
        await PickAsync("Criminal");
        await _page.GetByRole(AriaRole.Combobox).Nth(0).SelectOptionAsync(new SelectOptionValue { Label = "Intelligence" });
        await _page.GetByRole(AriaRole.Combobox).Nth(1).SelectOptionAsync(new SelectOptionValue { Label = "Constitution" });
        await NextAsync("Languages");
        await PickFirstAsync(2);
        await NextAsync("Ability Scores");
        await StandardArrayAsync();
        await NextAsync("Proficiencies");
        await PickFirstAsync(2);
        await NextAsync("Improvements");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Tough" }).ClickAsync();
        await NextAsync("Spells");
        await PickAsManyAsAllowedAsync("Cantrips");
        await PickAsManyAsAllowedAsync("Spells");
        await NextAsync("Review");
        await CreateAsync("Elara");

        // Assert
        await _page.GotoAsync("/characters");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Elara", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Wizard_WhenTheServerRefusesAPick_ShowsTheServersMessageOnTheStep()
    {
        // Arrange: the scores go out as a point buy no 27 points can pay for, so the real API
        // refuses them; the step has to show its reason rather than move on
        await RegisterAndSignInAsync("wizard-refused");
        await StartCharacterAsync("Grom", "2014 rules");
        await PickAsync("Half-Orc");
        await NextAsync("Class");
        await PickAsync("Fighter");
        await NextAsync("Background");
        await PickAsync("Soldier");
        await NextAsync("Languages");
        await NextAsync("Ability Scores");
        await StandardArrayAsync();

        await _page.RouteAsync("**/ability-scores", route => route.ContinueAsync(new RouteContinueOptions
        {
            PostData = JsonSerializer.SerializeToUtf8Bytes(new
            {
                scores = new { STR = 15, DEX = 15, CON = 15, INT = 15, WIS = 15, CHA = 15 },
                method = "point-buy",
            }),
        }));

        // Act
        var refusal = await _page.RunAndWaitForResponseAsync(
            () => _page.GetByRole(AriaRole.Button, new() { Name = "Next: Proficiencies" }).ClickAsync(),
            response => response.Url.EndsWith("/ability-scores", StringComparison.Ordinal));

        // Assert
        Assert.Equal(400, refusal.Status);
        var problem = await refusal.JsonAsync();
        var reason = problem!.Value.GetProperty("errors")[0].GetProperty("detail").GetString()!;
        await Expect(_page.GetByText($"Failed to save ability scores: {reason}")).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Step 6: Ability Scores" })).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);

    [GeneratedRegex("^/character/[0-9a-f-]{36}$")]
    private static partial Regex CharacterSheetPath();

    private async Task RegisterAndSignInAsync(string userName)
    {
        await _page.GotoAsync("/register");
        await _page.Locator("#username").FillAsync(userName);
        await _page.Locator("#email").FillAsync($"{userName}@example.com");
        await _page.Locator("#password").FillAsync(Password);
        await _page.Locator("#confirmPassword").FillAsync(Password);
        await _page.Locator("#inviteCode").FillAsync(WizardHost.InviteCode);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create Account" }).ClickAsync();

        await _page.WaitForURLAsync("**/login?registered=true");
        await _page.Locator("#email").FillAsync($"{userName}@example.com");
        await _page.Locator("#password").FillAsync(Password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await _page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.Ordinal));
    }

    private async Task StartCharacterAsync(string name, string rules)
    {
        await _page.GotoAsync("/character/create");
        await _page.GetByPlaceholder("Enter your character's name").FillAsync(name);
        await _page.GetByRole(AriaRole.Button, new() { Name = rules }).ClickAsync();
        await NextAsync("Race");
    }

    // A card on a step: a race, class or background, by the name it shows
    private Task PickAsync(string name) =>
        _page.GetByRole(AriaRole.Button, new() { Name = name }).First.ClickAsync();

    // The first options still open in a section of picks (skills, languages, spells)
    private async Task PickFirstAsync(int count, string? section = null)
    {
        var scope = section is null
            ? _page.Locator("body")
            : _page.Locator("div.rounded-lg", new() { Has = _page.GetByRole(AriaRole.Heading, new() { Name = section, Exact = true }) }).Last;

        // Picking one leaves the others open until the count is reached, so the indexes hold
        var open = scope.Locator("div.grid button:not([disabled]), div.space-y-2 > button:not([disabled])");

        for (var i = 0; i < count; i++)
        {
            await open.Nth(i).ClickAsync();
        }
    }

    // The spells step says how many the server allows ("Choose 6 (up to level 1)"); that many are picked
    private async Task PickAsManyAsAllowedAsync(string section)
    {
        var heading = _page.GetByRole(AriaRole.Heading, new() { Name = section, Exact = true });
        var allowed = await heading.Locator("xpath=..").Locator("p").First.InnerTextAsync();

        await PickFirstAsync(int.Parse(ChooseCount().Match(allowed).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), section);
    }

    [GeneratedRegex(@"^Choose (\d+)")]
    private static partial Regex ChooseCount();

    // The first tool of the one tool choice the step offers ("Soldier: choose 1 tool")
    private Task PickToolAsync() =>
        _page.Locator("h4", new() { HasTextRegex = new Regex("choose 1 tool") })
            .Locator("xpath=../..").Locator("button:not([disabled])").First.ClickAsync();

    private async Task StandardArrayAsync()
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Standard Array" }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Generate Standard Array" }).ClickAsync();

        foreach (var score in new[] { "15", "14", "13", "12", "10", "8" })
        {
            await _page.GetByRole(AriaRole.Button, new() { Name = score, Exact = true }).ClickAsync();
            await _page.GetByRole(AriaRole.Button, new() { Name = $"Assign {score}" }).First.ClickAsync();
        }
    }

    // Moves on only once the next step's heading shows, so the next pick is made on that step
    private async Task NextAsync(string step)
    {
        var number = WizardSteps.Names.ToList().IndexOf(step) + 1;

        await _page.GetByRole(AriaRole.Button, new() { Name = $"Next: {step}" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex($"^Step {number}: ") })).ToBeVisibleAsync();
    }

    private async Task CreateAsync(string name)
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create Character" }).ClickAsync();
        await _page.WaitForURLAsync(url => CharacterSheetPath().IsMatch(new Uri(url).AbsolutePath));
        await Expect(_page.GetByText(name).First).ToBeVisibleAsync();
    }
}
