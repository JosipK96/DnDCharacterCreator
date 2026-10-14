using CharacterCreatorApp.Infrastructure;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace CharacterCreatorApp.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Provides a real PostgreSQL database running in Docker for integration tests.
/// Implements IAsyncLifetime to manage container lifecycle.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public ApplicationDbContext DbContext { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // Create and start PostgreSQL container with specific version
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("charactercreator_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Create DbContext with container connection string
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        DbContext = new ApplicationDbContext(options);

        // Migrations rather than EnsureCreated: some of the schema is raw SQL in a migration,
        // like the deferrable keys that let SeedService replace the reference data
        await DbContext.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Cleans all data from the database between tests while preserving schema.
    /// This is faster than recreating the database for each test.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        // One TRUNCATE for every table, so the foreign keys between them cannot make the order
        // matter; the migrations history stays, the schema having been built from it
        await DbContext.Database.ExecuteSqlRawAsync("""
            DO $$
            DECLARE
                tables text;
            BEGIN
                SELECT string_agg(format('%I', tablename), ', ') INTO tables
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory';

                EXECUTE 'TRUNCATE ' || tables || ' RESTART IDENTITY CASCADE';
            END $$;
            """);

        DbContext.ChangeTracker.Clear();
    }

    /// <summary>
    /// Creates a new DbContext instance for a test.
    /// Useful when you need a fresh context without cached entities.
    /// </summary>
    public ApplicationDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container!.GetConnectionString())
            .Options;

        return new ApplicationDbContext(options);
    }
}