using System.IdentityModel.Tokens.Jwt;
using System.Text;

using CharacterCreatorApp.Infrastructure;
using CharacterCreatorApp.Infrastructure.Seed;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using Testcontainers.PostgreSql;

namespace CharacterCreatorApp.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that replaces the production database
/// with a PostgreSQL test container for API integration tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    internal const string TestKey = "ThisIsASecretKeyForTestingPurposesOnly12345678901234567890";
    internal const string TestIssuer = "CharacterCreatorTestIssuer";
    internal const string TestAudience = "CharacterCreatorTestAudience";

    private PostgreSqlContainer? _container;

    public async ValueTask InitializeAsync()
    {
        // Disable automatic claim type mapping so "sub" stays as "sub" instead of being mapped to NameIdentifier
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

        // Create and start PostgreSQL container
        // Neon's major version; keep in step with docker-compose.yml and the other two fixtures.
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("charactercreator_api_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Program.cs registers services as the WebApplicationBuilder is built, which happens
        // before ConfigureWebHost's callbacks are applied. The connection string and signing
        // key are required at that point, so they have to come from a source the builder reads
        // on its own: environment variables, the same way the app is configured in production.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _container.GetConnectionString());
        Environment.SetEnvironmentVariable("JwtSettings__Key", TestKey);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", TestIssuer);
        Environment.SetEnvironmentVariable("JwtSettings__Audience", TestAudience);
        Environment.SetEnvironmentVariable("JwtSettings__ExpiryInMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set test environment to skip startup initialization in Program.cs
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Clear existing configuration sources to avoid conflicts
            configBuilder.Sources.Clear();

            // Add test configuration
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = TestKey,
                ["JwtSettings:Issuer"] = TestIssuer,
                ["JwtSettings:Audience"] = TestAudience,
                ["JwtSettings:ExpiryInMinutes"] = "60",
                ["ConnectionStrings:DefaultConnection"] = _container!.GetConnectionString()
            }!);
        });

        // The app's own DbContext registration is kept: the environment variables set in
        // InitializeAsync already point it at the container, and replacing it here used to drop
        // AuditableEntityInterceptor, so no API test ran with the DbContext production uses.
        builder.ConfigureTestServices(services =>
        {
            // Use PostConfigure to ensure this runs AFTER AddJwtAuthorization
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
                    ClockSkew = TimeSpan.Zero // Remove clock skew for tests
                };
            });
        });

        // Initialize database AFTER all services are configured
        builder.ConfigureServices(services =>
        {
            // This runs after all service registrations are complete
            services.AddSingleton<IStartupFilter>(new DatabaseInitializationFilter(_container!.GetConnectionString()));
        });
    }

    /// <summary>
    /// Startup filter to initialize database after all services are registered
    /// </summary>
    private sealed class DatabaseInitializationFilter : IStartupFilter
    {
        private readonly string _connectionString;

        public DatabaseInitializationFilter(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Initialize database before running the app
                using var scope = app.ApplicationServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Run migrations
                db.Database.Migrate();

                // Seed data
                var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
                seedService.SeedData(CancellationToken.None).GetAwaiter().GetResult();

                next(app);
            };
        }
    }

    public new async Task DisposeAsync()
    {
        foreach (var name in new[]
        {
            "ConnectionStrings__DefaultConnection",
            "JwtSettings__Key",
            "JwtSettings__Issuer",
            "JwtSettings__Audience",
            "JwtSettings__ExpiryInMinutes"
        })
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        if (_container != null)
        {
            await _container.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    /// <summary>
    /// Helper method to reset the database between tests
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Delete user-created data but preserve seed data
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM ""Characters"";
            DELETE FROM ""CharacterDrafts"";
            DELETE FROM ""AspNetUsers"";
        ");
    }
}