using System.IdentityModel.Tokens.Jwt;

using CharacterCreatorApp.Api;
using CharacterCreatorApp.Application;
using CharacterCreatorApp.Contracts.Common;
using CharacterCreatorApp.Infrastructure;
using CharacterCreatorApp.Infrastructure.Seed;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddIdentity(builder.Configuration);
builder.Services.AddJwtAuthorization(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying pending migrations");
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync(app.Lifetime.ApplicationStopping);

        logger.LogInformation("Seeding reference data");
        await services.GetRequiredService<SeedService>().SeedData(app.Lifetime.ApplicationStopping);
    }
    catch (Exception ex)
    {
        // This used to swallow the failure outside Development, leaving an API that answered
        // every request with a database error. Not starting at all is easier to notice.
        logger.LogCritical(ex, "Database migration or seeding failed");
        throw;
    }
}

// First, so it also catches what the middleware below it throws
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

// This app serves the Blazor client as well, so the client calls the API on its own origin:
// no CORS policy, and nothing to configure about where the API lives. One container is also
// what a free host runs. MapStaticAssets serves the client's files, including the
// fingerprinted framework files under their plain names; UseBlazorFrameworkFiles must not be
// added beside it, because its /_framework branch never reaches the endpoint and answers 500.
app.MapStaticAssets();

// After the forwarded headers (ASPNETCORE_FORWARDEDHEADERS_ENABLED adds them first of all), so a
// limit applies to the caller's own address and not the proxy's
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// The version is what the release workflow built; Render only looks at the status code
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        version = BuildVersion.Of(typeof(Program).Assembly)
    })
});

// An unknown API route is a 404, not the client's index.html with a 200
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
