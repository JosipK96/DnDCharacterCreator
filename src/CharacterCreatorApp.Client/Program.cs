using System.IdentityModel.Tokens.Jwt;

using CharacterCreatorApp.Client;
using CharacterCreatorApp.Client.Services;
using CharacterCreatorApp.Client.Services.Auth;
using CharacterCreatorApp.Client.Services.ErrorReporting;
using CharacterCreatorApp.Contracts.Common;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// CharacterCreatorApp.Api serves this app, so the API is always on the origin it came from
var apiBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

// A WebAssembly app has one user and one DI scope, so these can be singletons, and must be: the
// HttpClient's token handler and the pages have to share one authentication state.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenStore>();
builder.Services.AddSingleton<JwtAuthenticationStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped(sp => new HttpClient(new BearerTokenHandler(
    sp.GetRequiredService<TokenStore>(),
    sp.GetRequiredService<JwtAuthenticationStateProvider>())
{
    InnerHandler = new HttpClientHandler()
})
{
    BaseAddress = apiBaseAddress
});

// Unhandled errors go to the API, which sends them to the error tracker (#77). A plain HttpClient:
// the endpoint is anonymous, and reporting must not depend on, or disturb, the signed-in one.
builder.Services.AddSingleton<ILoggerProvider>(sp => new ClientErrorLoggerProvider(
    new HttpClient { BaseAddress = apiBaseAddress },
    () => new Uri(sp.GetRequiredService<NavigationManager>().Uri).AbsolutePath,
    BuildVersion.Of(typeof(Program).Assembly)));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<CharacterDraftService>();
builder.Services.AddScoped<CharacterService>();
builder.Services.AddScoped<LookupService>();

await builder.Build().RunAsync();
