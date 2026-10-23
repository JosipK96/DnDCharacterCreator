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

        // A WebAssembly app's load downloads and starts the runtime, so a step after a page load
        // gets longer; the assertions' own default (5 seconds) is too short for it on a slow runner
        _context.SetDefaultTimeout(30_000);
        Assertions.SetDefaultExpectTimeout(30_000);
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

        // Act
        await CreateGromAsync();

        // Assert
        await _page.GotoAsync("/characters");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Edit_ThatRenamesTheCharacter_KeepsEveryOtherChoiceAndReplacesIt()
    {
        // Arrange
        await RegisterAndSignInAsync("wizard-edit");
        await CreateGromAsync();
        var sheet = new Uri(_page.Url).AbsolutePath;

        // Act: a new name, then every step as it opens: each has to hold what Grom was made
        // with, or its Next would stay disabled
        await _page.GetByRole(AriaRole.Button, new() { Name = "Edit Character" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Edit Grom" })).ToBeVisibleAsync();
        await Expect(_page.GetByPlaceholder("Enter your character's name")).ToHaveValueAsync("Grom");
        await _page.GetByPlaceholder("Enter your character's name").FillAsync("Grom the Bold");

        foreach (var step in WizardSteps.Names.Skip(1))
        {
            await NextAsync(step);
        }

        await _page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        // Assert: the same sheet, renamed, and one character on the list
        await _page.WaitForURLAsync($"**{sheet}");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom the Bold", Exact = true })).ToBeVisibleAsync();
        await Expect(_page.GetByText("Half-Orc").First).ToBeVisibleAsync();

        await _page.GotoAsync("/characters");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom the Bold", Exact = true })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom", Exact = true })).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task LevelUp_TwiceAsksOnlyWhatEachLevelBringsAndKeepsTheCharacter()
    {
        // Arrange
        await RegisterAndSignInAsync("wizard-level-up");
        await CreateGromAsync();
        var sheet = new Uri(_page.Url).AbsolutePath;

        // Act: a 2014 fighter's second level brings nothing but its hit points
        await _page.GetByRole(AriaRole.Button, new() { Name = "Level Up" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Level up Grom to level 2" })).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Step 1: Hit Points" })).ToBeVisibleAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Take 6" }).ClickAsync();
        await LevelUpNextAsync("Review", 2);
        await SaveLevelUpAsync(sheet, "Level 2 Fighter");

        // The third brings the subclass too, and the spells an Eldritch Knight would cast
        await _page.GetByRole(AriaRole.Button, new() { Name = "Level Up" }).ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Level up Grom to level 3" })).ToBeVisibleAsync();
        await _page.Locator("#hit-die-roll").FillAsync("8");
        await _page.Locator("#hit-die-roll").DispatchEventAsync("change");
        await LevelUpNextAsync("Class", 2);
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Change" })).ToHaveCountAsync(0);
        await PickAsync("Champion");
        await LevelUpNextAsync("Spells", 3);
        await Expect(_page.GetByText("No Spells Available")).ToBeVisibleAsync();
        await LevelUpNextAsync("Review", 4);
        await SaveLevelUpAsync(sheet, "Level 3 Fighter (Champion)");

        // Assert: still one Grom, at the new level
        await _page.GotoAsync("/characters");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Grom", Exact = true })).ToHaveCountAsync(1);
        await Expect(_page.GetByText("Level 3 · 2014 rules")).ToBeVisibleAsync();
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

        // The race's skills are a second page of the same step: wait for it, or the pick lands
        // on the size buttons still on screen (as it did on a slower CI runner)
        await Expect(_page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("^Step 2: .+ Skill Proficiencies$") })).ToBeVisibleAsync();
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

    // A half-orc fighter, with skills, the Soldier's tool and a Fighting Style to pick; ends on his sheet
    private async Task CreateGromAsync()
    {
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

        var next = _page.GetByRole(AriaRole.Button, new() { Name = $"Next: {step}" });

        // A pick left undone keeps Next disabled; say so now rather than after the click's timeout
        await Expect(next).ToBeEnabledAsync(new() { Timeout = 5_000 });
        await next.ClickAsync();

        // Either the next step shows, or this one shows why the server refused the save
        var heading = _page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex($"^Step {number}: ") });
        var refusal = _page.Locator("div.bg-red-900 p");
        await heading.Or(refusal).First.WaitForAsync();

        if (await refusal.IsVisibleAsync())
        {
            Assert.Fail($"Moving on to {step} was refused: {await refusal.InnerTextAsync()}");
        }
    }

    // A level-up numbers its few steps as it shows them: the step comes up as "Step {shownAs}"
    private async Task LevelUpNextAsync(string step, int shownAs)
    {
        var next = _page.GetByRole(AriaRole.Button, new() { Name = $"Next: {step}" });
        await Expect(next).ToBeEnabledAsync(new() { Timeout = 5_000 });
        await next.ClickAsync();

        var heading = _page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex($"^Step {shownAs}: ") });
        var refusal = _page.Locator("div.bg-red-900 p");
        await heading.Or(refusal).First.WaitForAsync();

        if (await refusal.IsVisibleAsync())
        {
            Assert.Fail($"Moving on to {step} was refused: {await refusal.InnerTextAsync()}");
        }
    }

    // Saving a level-up replaces the character, so the wizard goes back to the same sheet
    private async Task SaveLevelUpAsync(string sheet, string levelAndClass)
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
        await _page.WaitForURLAsync($"**{sheet}");
        await Expect(_page.GetByText(levelAndClass, new() { Exact = true })).ToBeVisibleAsync();
    }

    private async Task CreateAsync(string name)
    {
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create Character" }).ClickAsync();
        await _page.WaitForURLAsync(url => CharacterSheetPath().IsMatch(new Uri(url).AbsolutePath));
        await Expect(_page.GetByText(name).First).ToBeVisibleAsync();
    }
}
