using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

using Testcontainers.PostgreSql;

namespace CharacterCreatorApp.Client.IntegrationTests.Fixtures;

/// <summary>
/// The Api serving the Blazor client on a real Kestrel port, over a PostgreSQL container, and a
/// headless Chromium to drive it. TestServer has no port for a browser to open, which is why this
/// is not the Api tests' factory.
/// </summary>
public sealed class WizardHost : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>The code registration asks for, as a host sets it with Registration__InviteCode.</summary>
    public const string InviteCode = "mellon";

    // Read by Program.cs while the WebApplicationBuilder is built, before ConfigureWebHost runs,
    // so they are environment variables, as on a real host
    private static readonly string[] Settings =
    [
        "ConnectionStrings__DefaultConnection", "JwtSettings__Key", "JwtSettings__Issuer", "JwtSettings__Audience",
        "JwtSettings__ExpiryInMinutes", "Registration__InviteCode",
    ];

    // Neon's major version; keep in step with docker-compose.yml and the other two fixtures.
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("charactercreator_wizard_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();

    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;

    /// <summary>Where Kestrel listens, "http://127.0.0.1:{port}".</summary>
    public string Address { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _database.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("JwtSettings__Key", "ThisIsASecretKeyForTheWizardTestsOnly12345678901234567890");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "CharacterCreatorWizardTests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "CharacterCreatorWizardTests");
        Environment.SetEnvironmentVariable("JwtSettings__ExpiryInMinutes", "60");
        Environment.SetEnvironmentVariable("Registration__InviteCode", InviteCode);

        // Port 0: the OS picks a free one. The environment is not "Testing", so Program.cs
        // migrates and seeds the database itself, as it does on a host.
        UseKestrel(0);
        StartServer();

        Address = Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses
            .Single()
            .Replace("[::]", "127.0.0.1", StringComparison.Ordinal);

        // Installs Chromium on the first run and is a quick no-op after that; CI installs it, and
        // the libraries it needs, in a step of its own
        if (Microsoft.Playwright.Program.Main(["install", "chromium"]) != 0)
        {
            throw new InvalidOperationException("Playwright could not install Chromium.");
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public override async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await base.DisposeAsync();
        await _database.DisposeAsync();

        foreach (var name in Settings)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("WizardTests");

        // The client's files are served from where the build left them, which only Development
        // turns on by itself; a published app has them in wwwroot instead
        builder.UseStaticWebAssets();

        // Every SQL command at Information buries a failed test's message in the output
        builder.ConfigureLogging(logging => logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
    }
}
